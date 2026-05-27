using SCLabelPrinter.Core.Models;

namespace SCLabelPrinter.Core.Printing;

/// <summary>
/// 表示文本元素在预览画布上的布局约束结果。
/// </summary>
public sealed class TsplTextPreviewLayoutPlan
{
    public double FontSizeDots { get; init; }

    public double LineHeightDots { get; init; }

    public double MaxWidthDots { get; init; }

    public double MaxHeightDots { get; init; }

    public int MaxLines { get; init; }

    public int EstimatedLineCount { get; init; }

    public bool ShouldTrim { get; init; }
}

/// <summary>
/// 定义文本元素预览布局约束的规划接口。
/// </summary>
public interface ITsplTextPreviewLayoutPlanner
{
    /// <summary>
    /// 根据标签尺寸和文本元素属性规划预览边界与高度限制。
    /// </summary>
    TsplTextPreviewLayoutPlan Plan(LabelDefinition label, TextElement element);
}

/// <summary>
/// 基于标签剩余空间和字体尺寸规划文本元素的预览宽高约束。
/// </summary>
public sealed class TsplTextPreviewLayoutPlanner : ITsplTextPreviewLayoutPlanner
{
    private const double DotsPerMillimeter = 8.0;
    private const double MinPreviewWidthDots = 48.0;
    private const double RightPaddingDots = 22.0;
    private const double CharacterWidthFactor = 0.62;
    private const double LineHeightFactor = 1.35;
    private const int MaxPreviewLines = 4;
    private const int MinPreviewUnits = 8;
    private const int MaxPreviewUnits = 18;

    /// <summary>
    /// 根据标签尺寸和文本元素属性规划预览边界与高度限制。
    /// </summary>
    public TsplTextPreviewLayoutPlan Plan(LabelDefinition label, TextElement element)
    {
        ArgumentNullException.ThrowIfNull(label);
        ArgumentNullException.ThrowIfNull(element);

        var baseFontDots = MapTsplFontSize(element.Font);
        var horizontalScale = Math.Max(1, element.XScale);
        var verticalScale = Math.Max(1, element.YScale);
        var fontSizeDots = baseFontDots * verticalScale;
        var lineHeightDots = Math.Max(baseFontDots * verticalScale * LineHeightFactor, baseFontDots);
        var characterWidthDots = Math.Max(6.0, baseFontDots * horizontalScale * CharacterWidthFactor);
        var labelWidthDots = ConvertToDots(label.Width, label.Unit);
        var remainingWidthDots = Math.Max(1.0, labelWidthDots - Math.Max(0, element.X) - RightPaddingDots);
        var preferredUnits = Math.Clamp((int)Math.Ceiling(EstimateTextUnits(element.Content)), MinPreviewUnits, MaxPreviewUnits);
        var preferredWidthDots = characterWidthDots * preferredUnits + 12.0;
        var maxWidthDots = remainingWidthDots <= MinPreviewWidthDots
            ? remainingWidthDots
            : Math.Min(remainingWidthDots, Math.Max(MinPreviewWidthDots, preferredWidthDots));
        maxWidthDots = Math.Max(1.0, maxWidthDots);

        var maxCharactersPerLine = Math.Max(1, (int)Math.Floor(maxWidthDots / characterWidthDots));
        var estimatedLineCount = EstimateLineCount(element.Content, maxCharactersPerLine);

        return new TsplTextPreviewLayoutPlan
        {
            FontSizeDots = fontSizeDots,
            LineHeightDots = lineHeightDots,
            MaxWidthDots = maxWidthDots,
            MaxHeightDots = lineHeightDots * MaxPreviewLines,
            MaxLines = MaxPreviewLines,
            EstimatedLineCount = estimatedLineCount,
            ShouldTrim = estimatedLineCount > MaxPreviewLines,
        };
    }

    /// <summary>
    /// 将标签单位统一换算为点阵宽度，供预览规划使用。
    /// </summary>
    private static double ConvertToDots(double size, LabelUnit unit)
    {
        return unit == LabelUnit.Millimeter ? size * DotsPerMillimeter : size;
    }

    /// <summary>
    /// 估算文本内容的显示宽度权重，避免极长文本把预览宽度拉满。
    /// </summary>
    private static double EstimateTextUnits(string content)
    {
        if (string.IsNullOrEmpty(content))
        {
            return 1.0;
        }

        var units = 0.0;
        foreach (var ch in content)
        {
            units += GetCharacterUnits(ch);
        }

        return Math.Max(1.0, units);
    }

    /// <summary>
    /// 估算给定宽度下的换行数量，为最大显示行数裁剪提供依据。
    /// </summary>
    private static int EstimateLineCount(string content, int maxUnitsPerLine)
    {
        if (string.IsNullOrEmpty(content))
        {
            return 1;
        }

        var lines = 1;
        var currentUnits = 0.0;
        foreach (var ch in content)
        {
            if (ch == '\r')
            {
                continue;
            }

            if (ch == '\n')
            {
                lines++;
                currentUnits = 0.0;
                continue;
            }

            var charUnits = GetCharacterUnits(ch);
            if (currentUnits > 0 && currentUnits + charUnits > maxUnitsPerLine)
            {
                lines++;
                currentUnits = charUnits;
                continue;
            }

            currentUnits += charUnits;
        }

        return lines;
    }

    /// <summary>
    /// 根据字符类型估算其在预览中的横向占位权重。
    /// </summary>
    private static double GetCharacterUnits(char ch)
    {
        if (char.IsWhiteSpace(ch))
        {
            return 0.5;
        }

        if (ch <= 0x7F)
        {
            return char.IsLetterOrDigit(ch) ? 1.0 : 0.85;
        }

        return 2.0;
    }

    /// <summary>
    /// 将 TSPL 字体编号映射为预览布局计算使用的基础字号。
    /// </summary>
    private static double MapTsplFontSize(string font)
    {
        return font switch
        {
            "1" => 12,
            "2" => 16,
            "3" => 20,
            "4" => 26,
            "5" => 34,
            "6" => 42,
            _ => 18,
        };
    }
}
namespace SCLabelPrinter.Core.Models;

/// <summary>
/// 定义表格内部文本元素在预览场景下的字号与最小尺寸计算接口，避免工厂和绘制层各自维护硬编码规则。
/// </summary>
public interface ITableCellTextPreviewMetricsService
{
    /// <summary>
    /// 获取指定 TSPL 字体在预览中的基础字号。
    /// </summary>
    double GetBaseFontSize(string font);

    /// <summary>
    /// 获取考虑纵向缩放后的预览字号。
    /// </summary>
    double GetPreviewFontSize(string font, int yScale);

    /// <summary>
    /// 获取保证至少可显示一行文本的最小内容高度。
    /// </summary>
    double GetMinimumContentHeight(string font, int yScale);

    /// <summary>
    /// 获取包含内容内边距后的最小外框高度。
    /// </summary>
    int GetMinimumFrameHeight(string font, int yScale, int contentPadding);
}

/// <summary>
/// 提供表格内部文本元素的预览字号和最小高度规则，防止默认元素创建后即被排版系统压成零尺寸。
/// </summary>
public sealed class TableCellTextPreviewMetricsService : ITableCellTextPreviewMetricsService
{
    private const double SingleLineHeightFactor = 1.35d;

    /// <summary>
    /// 获取指定 TSPL 字体在预览中的基础字号。
    /// </summary>
    public double GetBaseFontSize(string font)
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

    /// <summary>
    /// 获取考虑纵向缩放后的预览字号。
    /// </summary>
    public double GetPreviewFontSize(string font, int yScale)
    {
        return GetBaseFontSize(font) * Math.Max(1, yScale);
    }

    /// <summary>
    /// 获取保证至少可显示一行文本的最小内容高度。
    /// </summary>
    public double GetMinimumContentHeight(string font, int yScale)
    {
        return Math.Ceiling(GetPreviewFontSize(font, yScale) * SingleLineHeightFactor);
    }

    /// <summary>
    /// 获取包含内容内边距后的最小外框高度。
    /// </summary>
    public int GetMinimumFrameHeight(string font, int yScale, int contentPadding)
    {
        var safePadding = Math.Max(0, contentPadding);
        return (int)GetMinimumContentHeight(font, yScale) + safePadding * 2;
    }
}
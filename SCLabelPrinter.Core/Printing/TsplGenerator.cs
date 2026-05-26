using SCLabelPrinter.Core.Models;

namespace SCLabelPrinter.Core.Printing;

/// <summary>
/// 负责将标签模板转换为完整的 TSPL 打印命令。
/// </summary>
public sealed class TsplGenerator
{
    private readonly IReadOnlyList<IElementTsplWriter> _writers;

    /// <summary>
    /// 创建 TSPL 生成器，并加载默认的元素输出策略。
    /// </summary>
    public TsplGenerator(IEnumerable<IElementTsplWriter>? writers = null)
    {
        var resolvedWriters = writers?.ToArray();
        _writers = resolvedWriters is { Length: > 0 }
            ? resolvedWriters
            : CreateDefaultWriters();
    }

    /// <summary>
    /// 创建内置的默认元素写入器集合，确保未配置依赖注入时仍可正常生成 TSPL。
    /// </summary>
    private static IReadOnlyList<IElementTsplWriter> CreateDefaultWriters()
    {
        return
        [
            new TextElementTsplWriter(),
            new BarcodeElementTsplWriter(),
            new QrCodeElementTsplWriter(),
            new BoxElementTsplWriter(),
            new LineElementTsplWriter(),
            new EraseElementTsplWriter(),
        ];
    }

    /// <summary>
    /// 根据标签模板生成 TSPL 指令文本。
    /// </summary>
    public string Generate(LabelTemplateDocument template, int copies = 1)
    {
        ArgumentNullException.ThrowIfNull(template);

        if (copies <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(copies), copies, "打印份数必须大于 0。");
        }

        var lines = new List<string>
        {
            $"SIZE {TsplValueFormatter.FormatNumber(template.Label.Width)} {TsplValueFormatter.MapUnit(template.Label.Unit)},{TsplValueFormatter.FormatNumber(template.Label.Height)} {TsplValueFormatter.MapUnit(template.Label.Unit)}",
            $"GAP {TsplValueFormatter.FormatNumber(template.Label.Gap)} {TsplValueFormatter.MapUnit(template.Label.Unit)}",
            $"DENSITY {template.Label.Density}",
            "CLS",
        };

        foreach (var element in template.Elements)
        {
            var writer = _writers.FirstOrDefault(candidate => candidate.CanWrite(element));
            if (writer is null)
            {
                throw new NotSupportedException($"当前未配置元素类型 {element.GetType().Name} 的 TSPL 写入器。");
            }

            lines.Add(writer.Write(element));
        }

        lines.Add($"PRINT {copies}");

        return string.Join("\r\n", lines) + "\r\n";
    }
}
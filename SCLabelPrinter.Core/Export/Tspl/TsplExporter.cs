using SCLabelPrinter.Core.Models;
using SCLabelPrinter.Core.Printing;

namespace SCLabelPrinter.Core.Export.Tspl;

/// <summary>
/// TSPL 标签导出器，将 LabelTemplateDocument 转换为 TSPL 指令。
/// 通过组合多个 IElementWriter 实现对各种元素类型的导出。
/// </summary>
public sealed class TsplExporter : ILabelExporter
{
    private readonly IReadOnlyList<IElementWriter> _writers;

    /// <summary>
    /// 创建 TSPL 导出器，注入所有 TSPL 语言的元素写入器。
    /// </summary>
    public TsplExporter(IEnumerable<IElementWriter> writers)
    {
        _writers = writers.Where(w => w.Language == PrinterLanguage.Tspl).ToArray();
    }

    /// <summary>
    /// 创建 TSPL 导出器，使用默认内置写入器集合。
    /// </summary>
    public TsplExporter()
    {
        _writers = CreateDefaultWriters();
    }

    public PrinterLanguage Language => PrinterLanguage.Tspl;

    public string DisplayName => "TSPL";

    /// <summary>
    /// 将标签模板导出为 TSPL 指令字节数组。
    /// </summary>
    public byte[] Export(LabelTemplateDocument template, ExportOptions options)
    {
        var builder = BuildCommands(template, options);
        return builder.ToByteArray();
    }

    /// <summary>
    /// 将标签模板导出为 TSPL 指令文本。
    /// </summary>
    public string ExportText(LabelTemplateDocument template, ExportOptions options)
    {
        var builder = BuildCommands(template, options);
        return builder.ToText();
    }

    /// <summary>
    /// 构建 TSPL 指令流。
    /// </summary>
    private TextCommandBuilder BuildCommands(LabelTemplateDocument template, ExportOptions options)
    {
        ArgumentNullException.ThrowIfNull(template);

        var builder = new TextCommandBuilder();

        builder.AppendLine($"SIZE {TsplValueFormatter.FormatNumber(template.Label.Width)} {TsplValueFormatter.MapUnit(template.Label.Unit)},{TsplValueFormatter.FormatNumber(template.Label.Height)} {TsplValueFormatter.MapUnit(template.Label.Unit)}");
        builder.AppendLine($"GAP {TsplValueFormatter.FormatNumber(template.Label.Gap)} {TsplValueFormatter.MapUnit(template.Label.Unit)}");
        builder.AppendLine($"DENSITY {options.Density}");
        builder.AppendLine("CLS");

        foreach (var element in template.Elements)
        {
            var writer = _writers.FirstOrDefault(w => w.CanWrite(element));
            if (writer is null)
            {
                throw new NotSupportedException($"当前未配置元素类型 {element.GetType().Name} 的 TSPL 写入器。");
            }

            writer.Write(element, builder);
        }

        builder.AppendLine($"PRINT {options.Copies}");
        return builder;
    }

    /// <summary>
    /// 创建内置的默认写入器集合。
    /// </summary>
    private static IReadOnlyList<IElementWriter> CreateDefaultWriters()
    {
        return
        [
            new TsplTextElementWriter(),
            new TsplBarcodeElementWriter(),
            new TsplQrCodeElementWriter(),
            new TsplBoxElementWriter(),
            new TsplLineElementWriter(),
            new TsplEraseElementWriter(),
            new TsplTableElementWriter(),
        ];
    }
}
using SCLabelPrinter.Core.Models;

namespace SCLabelPrinter.Core.Export.Tspl;

/// <summary>
/// TSPL 模板导出器，将含占位符的元素导出为带占位标记的 TSPL 文本。
/// 占位符格式为 {{占位符名称}}，方便外部软件按名称进行批量替换。
/// 每个占位符在输出中占据固定位置（行号），外部软件可按行号或占位符名称定位替换。
/// </summary>
public sealed class TsplTemplateExporter
{
    private readonly TsplExporter _exporter;

    public TsplTemplateExporter()
    {
        _exporter = new TsplExporter();
    }

    /// <summary>
    /// 导出 TSPL 模板文本。含占位符的元素用 {{name}} 替代实际内容。
    /// </summary>
    public string ExportTemplate(LabelTemplateDocument template, ExportOptions? options = null)
    {
        var opts = options ?? new ExportOptions();
        var originalContents = new Dictionary<string, string>();

        foreach (var element in template.Elements)
        {
            var placeholder = GetPlaceholder(element);
            if (string.IsNullOrEmpty(placeholder))
            {
                continue;
            }

            var content = GetContent(element);
            originalContents[element.Id] = content;
            SetContent(element, "{{" + placeholder + "}}");
        }

        var result = _exporter.ExportText(template, opts);

        foreach (var element in template.Elements)
        {
            if (originalContents.TryGetValue(element.Id, out var original))
            {
                SetContent(element, original);
            }
        }

        return result;
    }

    /// <summary>
    /// 导出占位符清单，包含名称、类型和在 TSPL 中的行号。
    /// </summary>
    public List<PlaceholderInfo> GetPlaceholderList(LabelTemplateDocument template, ExportOptions? options = null)
    {
        var templateText = ExportTemplate(template, options);
        var lines = templateText.Split((char)10);

        var result = new List<PlaceholderInfo>();

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            var startIdx = line.IndexOf("{{");
            var endIdx = line.IndexOf("}}");
            if (startIdx >= 0 && endIdx > startIdx)
            {
                var name = line.Substring(startIdx + 2, endIdx - startIdx - 2);
                var elementType = line.StartsWith("TEXT") ? "Text"
                    : line.StartsWith("BARCODE") ? "Barcode"
                    : line.StartsWith("QRCODE") ? "QrCode"
                    : "Unknown";

                result.Add(new PlaceholderInfo
                {
                    Name = name,
                    LineNumber = i + 1,
                    ElementType = elementType,
                    TsplLine = line,
                });
            }
        }

        return result;
    }

    private static string GetPlaceholder(LabelElement element)
    {
        return element switch
        {
            TextElement t => t.Placeholder,
            BarcodeElement b => b.Placeholder,
            QrCodeElement q => q.Placeholder,
            _ => string.Empty,
        };
    }

    private static string GetContent(LabelElement element)
    {
        return element switch
        {
            TextElement t => t.Content,
            BarcodeElement b => b.Content,
            QrCodeElement q => q.Content,
            _ => string.Empty,
        };
    }

    private static void SetContent(LabelElement element, string value)
    {
        switch (element)
        {
            case TextElement t: t.Content = value; break;
            case BarcodeElement b: b.Content = value; break;
            case QrCodeElement q: q.Content = value; break;
        }
    }
}

/// <summary>
/// 占位符信息，描述一个占位符在 TSPL 模板中的位置和类型。
/// </summary>
public sealed class PlaceholderInfo
{
    /// <summary>占位符名称</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>在 TSPL 输出中的行号（从 1 开始）</summary>
    public int LineNumber { get; set; }

    /// <summary>元素类型（Text/Barcode/QrCode）</summary>
    public string ElementType { get; set; } = string.Empty;

    /// <summary>完整的 TSPL 指令行（含占位符）</summary>
    public string TsplLine { get; set; } = string.Empty;
}

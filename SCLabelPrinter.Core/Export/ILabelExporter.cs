using SCLabelPrinter.Core.Models;

namespace SCLabelPrinter.Core.Export;

/// <summary>
/// 标签导出器接口，负责将完整的 LabelTemplateDocument 转换为特定打印机语言的指令。
/// 每种打印机语言实现一个导出器，通过 IExporterFactory 按需获取。
/// </summary>
public interface ILabelExporter
{
    /// <summary>
    /// 当前导出器对应的打印机语言。
    /// </summary>
    PrinterLanguage Language { get; }

    /// <summary>
    /// 导出器的可读名称，用于界面展示。
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// 将标签模板文档导出为打印机指令字节数组。
    /// </summary>
    byte[] Export(LabelTemplateDocument template, ExportOptions options);

    /// <summary>
    /// 将标签模板文档导出为打印机指令文本（仅适用于文本协议）。
    /// </summary>
    string ExportText(LabelTemplateDocument template, ExportOptions options);
}
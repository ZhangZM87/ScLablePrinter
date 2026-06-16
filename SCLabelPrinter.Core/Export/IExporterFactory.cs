namespace SCLabelPrinter.Core.Export;

/// <summary>
/// 导出器工厂接口，根据打印机语言获取对应的 ILabelExporter 实现。
/// 通过 DI 注册所有 ILabelExporter 实现后，工厂自动聚合并按语言分发。
/// </summary>
public interface IExporterFactory
{
    /// <summary>
    /// 获取指定打印机语言的导出器。
    /// </summary>
    ILabelExporter GetExporter(PrinterLanguage language);

    /// <summary>
    /// 获取当前已注册的所有打印机语言列表。
    /// </summary>
    IReadOnlyList<PrinterLanguage> SupportedLanguages { get; }
}
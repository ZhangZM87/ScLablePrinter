namespace SCLabelPrinter.Core.Export;

/// <summary>
/// 导出器工厂实现，通过 DI 注入所有 ILabelExporter 实例后按语言分发。
/// </summary>
public sealed class ExporterFactory : IExporterFactory
{
    private readonly IReadOnlyDictionary<PrinterLanguage, ILabelExporter> _exporters;

    /// <summary>
    /// 创建导出器工厂，注入所有已注册的 ILabelExporter 实例。
    /// </summary>
    public ExporterFactory(IEnumerable<ILabelExporter> exporters)
    {
        _exporters = exporters.ToDictionary(e => e.Language);
    }

    public IReadOnlyList<PrinterLanguage> SupportedLanguages => _exporters.Keys.ToList();

    /// <summary>
    /// 获取指定打印机语言的导出器，不存在则抛出 NotSupportedException。
    /// </summary>
    public ILabelExporter GetExporter(PrinterLanguage language)
    {
        return _exporters.TryGetValue(language, out var exporter)
            ? exporter
            : throw new NotSupportedException($"不支持的打印机语言: {language}");
    }
}

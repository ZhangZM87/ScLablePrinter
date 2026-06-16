namespace SCLabelPrinter.Core.Export;

/// <summary>
/// 导出选项，控制打印份数、浓度等参数。
/// </summary>
public sealed class ExportOptions
{
    /// <summary>
    /// 打印份数，默认为 1。
    /// </summary>
    public int Copies { get; set; } = 1;

    /// <summary>
    /// 打印浓度（1-15），默认为 8。
    /// </summary>
    public int Density { get; set; } = 8;
}
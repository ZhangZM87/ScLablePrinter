namespace SCLabelPrinter.Core.Printers;

/// <summary>
/// 表示打印机当前抽象状态。
/// </summary>
public enum PrinterState
{
    Unknown,
    Ready,
    CoverOpen,
    OutOfPaper,
    RibbonMissing,
    Paused,
    Overheated,
    CoverOpenAndOutOfPaper,
    CoverOpenAndOutOfPaperAndRibbonMissing,
    OutOfPaperAndRibbonMissing,
    CoverOpenAndRibbonMissing,
}

/// <summary>
/// 表示一次状态查询结果。
/// </summary>
public readonly record struct PrinterStatusInfo(PrinterState State, string Description, byte RawCode);

/// <summary>
/// 负责解析赛创标签打印机返回的 TSPL 状态码。
/// </summary>
public sealed class PrinterStatusInterpreter
{
    private static readonly IReadOnlyDictionary<byte, PrinterStatusInfo> TsplStatusMap = new Dictionary<byte, PrinterStatusInfo>
    {
        [0x00] = new(PrinterState.Ready, "正常待机", 0x00),
        [0x01] = new(PrinterState.CoverOpen, "开盖", 0x01),
        [0x04] = new(PrinterState.OutOfPaper, "缺纸", 0x04),
        [0x05] = new(PrinterState.CoverOpenAndOutOfPaper, "开盖、缺纸", 0x05),
        [0x08] = new(PrinterState.RibbonMissing, "未装碳带", 0x08),
        [0x09] = new(PrinterState.CoverOpenAndRibbonMissing, "开盖、无碳带", 0x09),
        [0x0C] = new(PrinterState.OutOfPaperAndRibbonMissing, "缺纸、无碳带", 0x0C),
        [0x0D] = new(PrinterState.CoverOpenAndOutOfPaperAndRibbonMissing, "开盖、缺纸、无碳带", 0x0D),
        [0x10] = new(PrinterState.Paused, "暂停打印", 0x10),
        [0x41] = new(PrinterState.CoverOpen, "开盖", 0x41),
        [0x45] = new(PrinterState.CoverOpenAndOutOfPaper, "开盖、缺纸", 0x45),
        [0x80] = new(PrinterState.Overheated, "打印头过热", 0x80),
    };

    /// <summary>
    /// 将 TSPL 状态字节转换为上层可消费的结构化状态信息。
    /// </summary>
    public PrinterStatusInfo InterpretTsplStatus(byte statusCode)
    {
        return TsplStatusMap.TryGetValue(statusCode, out var status)
            ? status
            : new PrinterStatusInfo(PrinterState.Unknown, "未知", statusCode);
    }
}
namespace SCLabelPrinter.Core.Printers;

/// <summary>
/// 定义打印机发现、连接、状态查询和数据发送的统一接口。
/// </summary>
public interface IPrinterService
{
    bool IsConnected { get; }

    PrinterInfo? CurrentPrinter { get; }

    /// <summary>
    /// 扫描当前环境中可用的打印机设备。
    /// </summary>
    Task<IReadOnlyList<PrinterInfo>> DiscoverAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 连接指定的打印机设备。
    /// </summary>
    Task<bool> ConnectAsync(PrinterInfo printer, CancellationToken cancellationToken = default);

    /// <summary>
    /// 断开当前已连接的打印机。
    /// </summary>
    Task DisconnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 查询当前打印机的状态信息。
    /// </summary>
    Task<PrinterStatusInfo> QueryStatusAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 查询当前打印机返回的型号文本。
    /// </summary>
    Task<string> QueryModelAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 向当前打印机发送文本命令。
    /// </summary>
    Task SendCommandAsync(string command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 向当前打印机发送二进制数据。
    /// </summary>
    Task SendDataAsync(byte[] data, CancellationToken cancellationToken = default);
}
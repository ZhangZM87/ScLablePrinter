namespace SCLabelPrinter.Core.Printers;

/// <summary>
/// 表示可供选择的打印机设备信息。
/// </summary>
public sealed class PrinterInfo
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string DisplayName { get; set; } = string.Empty;

    public string DevicePath { get; set; } = string.Empty;

    public int DeviceIndex { get; set; }
}
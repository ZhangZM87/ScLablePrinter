namespace SCLabelPrinter.Infrastructure.Native;

/// <summary>
/// 提供对赛创 USB SDK 的最小封装入口，隔离上层对第三方程序集的直接依赖。
/// </summary>
public sealed class UsbInterop
{
    /// <summary>
    /// 创建底层 USB 操作对象，用于后续打印机通信服务接入。
    /// </summary>
    public libUsbContorl.UsbOperation CreateOperation()
    {
        return new libUsbContorl.UsbOperation();
    }
}
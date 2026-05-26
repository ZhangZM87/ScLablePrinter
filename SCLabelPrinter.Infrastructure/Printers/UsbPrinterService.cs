using System.Text;
using SCLabelPrinter.Core.Printers;
using SCLabelPrinter.Core.Printing;
using SCLabelPrinter.Infrastructure.Native;

namespace SCLabelPrinter.Infrastructure.Printers;

/// <summary>
/// 提供基于赛创 USB SDK 的打印机服务实现。
/// </summary>
public sealed class UsbPrinterService : IPrinterService
{
    private static readonly byte[] StatusCommand = [0x1B, 0x21, 0x3F, 0x0D, 0x0A];
    private static readonly byte[] ModelCommand = [0x7E, 0x21, 0x54];

    private readonly UsbInterop _usbInterop;
    private readonly PrinterStatusInterpreter _statusInterpreter;
    private readonly PrintDataChunker _chunker;
    private readonly SemaphoreSlim _syncLock = new(1, 1);
    private readonly libUsbContorl.UsbOperation _usbOperation;

    /// <summary>
    /// 创建 USB 打印机服务。
    /// </summary>
    public UsbPrinterService(UsbInterop usbInterop, PrinterStatusInterpreter statusInterpreter, PrintDataChunker chunker)
    {
        _usbInterop = usbInterop;
        _statusInterpreter = statusInterpreter;
        _chunker = chunker;
        _usbOperation = _usbInterop.CreateOperation();
    }

    public bool IsConnected { get; private set; }

    public PrinterInfo? CurrentPrinter { get; private set; }

    /// <summary>
    /// 枚举当前可访问的 USB 打印机。
    /// </summary>
    public async Task<IReadOnlyList<PrinterInfo>> DiscoverAsync(CancellationToken cancellationToken = default)
    {
        await _syncLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            _usbOperation.FindUSBPrinter();
            var devices = _usbOperation.mCurrentDevicePath.ToArray();
            var printers = new List<PrinterInfo>();

            for (var index = 0; index < _usbOperation.USBPortCount && index < devices.Length; index++)
            {
                var devicePath = devices[index];
                printers.Add(new PrinterInfo
                {
                    DeviceIndex = index,
                    DevicePath = devicePath,
                    DisplayName = $"USB {index + 1} - {devicePath}",
                    Id = $"usb-{index}",
                });
            }

            return printers;
        }
        finally
        {
            _syncLock.Release();
        }
    }

    /// <summary>
    /// 连接指定的 USB 打印机。
    /// </summary>
    public async Task<bool> ConnectAsync(PrinterInfo printer, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(printer);

        await _syncLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (IsConnected)
            {
                _usbOperation.CloseUSBPort();
                IsConnected = false;
                CurrentPrinter = null;
            }

            var linked = _usbOperation.LinkUSB(printer.DeviceIndex);
            if (linked)
            {
                IsConnected = true;
                CurrentPrinter = printer;
            }

            return linked;
        }
        finally
        {
            _syncLock.Release();
        }
    }

    /// <summary>
    /// 断开当前打印机连接。
    /// </summary>
    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        await _syncLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (IsConnected)
            {
                _usbOperation.CloseUSBPort();
                IsConnected = false;
                CurrentPrinter = null;
            }
        }
        finally
        {
            _syncLock.Release();
        }
    }

    /// <summary>
    /// 查询当前打印机状态。
    /// </summary>
    public async Task<PrinterStatusInfo> QueryStatusAsync(CancellationToken cancellationToken = default)
    {
        await _syncLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            EnsureConnected();
            _usbOperation.SendData2USB(StatusCommand, StatusCommand.Length);
            byte[]? received = null;
            _usbOperation.ReadDataFmUSB(ref received);
            return received is { Length: > 0 }
                ? _statusInterpreter.InterpretTsplStatus(received[0])
                : new PrinterStatusInfo(PrinterState.Unknown, "未知", 0xFF);
        }
        finally
        {
            _syncLock.Release();
        }
    }

    /// <summary>
    /// 查询当前打印机型号信息。
    /// </summary>
    public async Task<string> QueryModelAsync(CancellationToken cancellationToken = default)
    {
        await _syncLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            EnsureConnected();
            _usbOperation.SendData2USB(ModelCommand, ModelCommand.Length);
            byte[]? received = null;
            _usbOperation.ReadDataFmUSB(ref received);
            return received is { Length: > 0 }
                ? Encoding.Default.GetString(received).Trim('\0', '\r', '\n')
                : string.Empty;
        }
        finally
        {
            _syncLock.Release();
        }
    }

    /// <summary>
    /// 发送 TSPL 文本命令到当前打印机。
    /// </summary>
    public Task SendCommandAsync(string command, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(command);
        return SendDataAsync(Encoding.GetEncoding(54936).GetBytes(command), cancellationToken);
    }

    /// <summary>
    /// 按分包规则发送二进制数据到当前打印机。
    /// </summary>
    public async Task SendDataAsync(byte[] data, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(data);

        await _syncLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            EnsureConnected();
            var packets = _chunker.Split(data, 3072);
            foreach (var packet in packets)
            {
                cancellationToken.ThrowIfCancellationRequested();
                _usbOperation.SendData2USB(packet, packet.Length);
                if (packet.Length == 3072)
                {
                    await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    await Task.Delay(200, cancellationToken).ConfigureAwait(false);
                }
            }
        }
        finally
        {
            _syncLock.Release();
        }
    }

    /// <summary>
    /// 校验当前服务是否已经建立打印机连接。
    /// </summary>
    private void EnsureConnected()
    {
        if (!IsConnected || CurrentPrinter is null)
        {
            throw new InvalidOperationException("当前没有已连接的打印机。");
        }
    }
}
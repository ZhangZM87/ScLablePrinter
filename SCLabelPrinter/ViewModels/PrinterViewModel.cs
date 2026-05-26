using System.Collections.ObjectModel;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SCLabelPrinter.Core.Printers;
using SCLabelPrinter.Services;

namespace SCLabelPrinter.ViewModels;

/// <summary>
/// 提供打印机列表、连接和状态查询等页面行为。
/// </summary>
public partial class PrinterViewModel : ObservableObject
{
    private readonly IPrinterService _printerService;
    private readonly StatusCenter _statusCenter;
    private readonly DispatcherTimer _statusTimer;

    /// <summary>
    /// 创建打印机管理视图模型。
    /// </summary>
    public PrinterViewModel(IPrinterService printerService, StatusCenter statusCenter)
    {
        _printerService = printerService;
        _statusCenter = statusCenter;
        _statusTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(3),
        };
        _statusTimer.Tick += StatusTimerOnTick;
    }

    public ObservableCollection<PrinterInfo> Printers { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConnectCommand))]
    private PrinterInfo? selectedPrinter;

    [ObservableProperty]
    private PrinterState currentState = PrinterState.Unknown;

    [ObservableProperty]
    private string currentStatusText = "未查询";

    [ObservableProperty]
    private string currentModelText = "-";

    [ObservableProperty]
    private bool isConnected;

    [ObservableProperty]
    private bool isBusy;

    /// <summary>
    /// 扫描当前可用的打印机列表。
    /// </summary>
    [RelayCommand]
    private async Task ScanAsync()
    {
        IsBusy = true;
        try
        {
            Printers.Clear();
            var printers = await _printerService.DiscoverAsync();
            foreach (var printer in printers)
            {
                Printers.Add(printer);
            }

            SelectedPrinter = Printers.FirstOrDefault();
            _statusCenter.SetActivityMessage($"已扫描到 {Printers.Count} 台打印机");
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// 连接当前选择的打印机。
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanConnect))]
    private async Task ConnectAsync()
    {
        if (SelectedPrinter is null)
        {
            return;
        }

        IsBusy = true;
        try
        {
            IsConnected = await _printerService.ConnectAsync(SelectedPrinter);
            CurrentStatusText = IsConnected ? "连接成功" : "连接失败";
            _statusCenter.SetPrinterMessage(IsConnected ? $"打印机: {SelectedPrinter.DisplayName}" : "打印机: 连接失败");
            _statusCenter.SetActivityMessage(CurrentStatusText);
            if (IsConnected)
            {
                _statusTimer.Start();
                await QueryStatusAsync();
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// 断开当前打印机连接。
    /// </summary>
    [RelayCommand]
    private async Task DisconnectAsync()
    {
        _statusTimer.Stop();
        await _printerService.DisconnectAsync();
        IsConnected = false;
        CurrentState = PrinterState.Unknown;
        CurrentStatusText = "已断开";
        CurrentModelText = "-";
        _statusCenter.SetPrinterMessage("打印机: 未连接");
        _statusCenter.SetActivityMessage("已断开当前打印机");
    }

    /// <summary>
    /// 查询当前打印机状态。
    /// </summary>
    [RelayCommand]
    private async Task QueryStatusAsync()
    {
        if (!IsConnected)
        {
            _statusCenter.SetActivityMessage("请先连接打印机后再查询状态");
            return;
        }

        var status = await _printerService.QueryStatusAsync();
        CurrentState = status.State;
        CurrentStatusText = status.Description;
        _statusCenter.SetActivityMessage($"打印机状态: {status.Description}");
    }

    /// <summary>
    /// 查询当前打印机型号。
    /// </summary>
    [RelayCommand]
    private async Task QueryModelAsync()
    {
        if (!IsConnected)
        {
            _statusCenter.SetActivityMessage("请先连接打印机后再查询型号");
            return;
        }

        CurrentModelText = await _printerService.QueryModelAsync();
        _statusCenter.SetActivityMessage(string.IsNullOrWhiteSpace(CurrentModelText) ? "未获取到型号文本" : $"打印机型号: {CurrentModelText}");
    }

    /// <summary>
    /// 判断当前是否满足连接命令的执行条件。
    /// </summary>
    private bool CanConnect()
    {
        return SelectedPrinter is not null && !IsBusy;
    }

    /// <summary>
    /// 在定时器触发时自动轮询当前打印机状态。
    /// </summary>
    private async void StatusTimerOnTick(object? sender, EventArgs e)
    {
        if (!IsConnected || IsBusy)
        {
            return;
        }

        try
        {
            var status = await _printerService.QueryStatusAsync();
            CurrentState = status.State;
            CurrentStatusText = status.Description;
            _statusCenter.SetActivityMessage($"自动轮询状态: {status.Description}");
        }
        catch
        {
            _statusTimer.Stop();
            IsConnected = false;
            CurrentState = PrinterState.Unknown;
            CurrentStatusText = "状态轮询失败";
            _statusCenter.SetPrinterMessage("打印机: 连接异常");
            _statusCenter.SetActivityMessage("自动轮询失败，请重新连接打印机");
        }
    }
}
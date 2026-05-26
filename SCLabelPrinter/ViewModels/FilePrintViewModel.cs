using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SCLabelPrinter.Core.Printers;
using SCLabelPrinter.Core.Storage;
using SCLabelPrinter.Services;

namespace SCLabelPrinter.ViewModels;

/// <summary>
/// 提供直接打开文件并发送到打印机的页面行为。
/// </summary>
public partial class FilePrintViewModel : ObservableObject
{
    private readonly IPrintFileService _printFileService;
    private readonly IPrinterService _printerService;
    private readonly IFileDialogService _fileDialogService;
    private readonly IUserNotificationService _notificationService;
    private readonly StatusCenter _statusCenter;

    /// <summary>
    /// 创建文件打印视图模型。
    /// </summary>
    public FilePrintViewModel(IPrintFileService printFileService, IPrinterService printerService, IFileDialogService fileDialogService, IUserNotificationService notificationService, StatusCenter statusCenter)
    {
        _printFileService = printFileService;
        _printerService = printerService;
        _fileDialogService = fileDialogService;
        _notificationService = notificationService;
        _statusCenter = statusCenter;
    }

    [ObservableProperty]
    private string filePath = string.Empty;

    [ObservableProperty]
    private int copies = 1;

    [ObservableProperty]
    private string payloadSummary = "尚未选择文件";

    [ObservableProperty]
    private string progressMessage = "等待打印";

    [ObservableProperty]
    private bool isBusy;

    /// <summary>
    /// 选择要直接打印的文件。
    /// </summary>
    [RelayCommand]
    private void BrowseFile()
    {
        if (_fileDialogService.TryOpenFile("支持文件|*.sclabel;*.prn;*.bin;*.txt|所有文件|*.*", out var path))
        {
            FilePath = path;
            PayloadSummary = $"待打印文件: {Path.GetFileName(path)}";
            _statusCenter.SetActivityMessage($"已选择文件 {Path.GetFileName(path)}");
        }
    }

    /// <summary>
    /// 加载并发送当前选择的文件到打印机。
    /// </summary>
    [RelayCommand]
    private async Task PrintAsync()
    {
        if (string.IsNullOrWhiteSpace(FilePath))
        {
            _statusCenter.SetActivityMessage("请先选择要打印的文件");
            return;
        }

        if (!_printerService.IsConnected)
        {
            _statusCenter.SetActivityMessage("请先连接打印机再执行文件打印");
            return;
        }

        IsBusy = true;
        try
        {
            ProgressMessage = "正在生成打印数据...";
            var payload = await _printFileService.LoadPayloadAsync(FilePath, Copies);
            PayloadSummary = $"类型: {payload.ContentType} | 大小: {payload.Data.Length} 字节";

            ProgressMessage = "正在发送到打印机...";
            await _printerService.SendDataAsync(payload.Data);

            ProgressMessage = "发送完成";
            _statusCenter.SetActivityMessage($"文件打印完成: {Path.GetFileName(FilePath)}");
        }
        catch (Exception ex)
        {
            _notificationService.ShowError($"文件打印失败: {ex.Message}");
            _statusCenter.SetActivityMessage("文件打印失败");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
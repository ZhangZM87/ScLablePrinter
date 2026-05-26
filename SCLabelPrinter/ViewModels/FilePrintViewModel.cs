using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SCLabelPrinter.Core.Models;
using SCLabelPrinter.Core.Printing;
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
    private readonly ILabelTemplateStorageService _labelTemplateStorageService;
    private readonly ITsplParser _tsplParser;
    private readonly StatusCenter _statusCenter;

    /// <summary>
    /// 创建文件打印视图模型。
    /// </summary>
    public FilePrintViewModel(IPrintFileService printFileService, IPrinterService printerService, IFileDialogService fileDialogService, IUserNotificationService notificationService, ILabelTemplateStorageService labelTemplateStorageService, ITsplParser tsplParser, StatusCenter statusCenter)
    {
        _printFileService = printFileService;
        _printerService = printerService;
        _fileDialogService = fileDialogService;
        _notificationService = notificationService;
        _labelTemplateStorageService = labelTemplateStorageService;
        _tsplParser = tsplParser;
        _statusCenter = statusCenter;
    }

    [ObservableProperty]
    private string filePath = string.Empty;

    [ObservableProperty]
    private int copies = 1;

    [ObservableProperty]
    private string payloadSummary = "尚未选择文件";

    [ObservableProperty]
    private LabelTemplateDocument? filePreviewTemplate;

    [ObservableProperty]
    private string filePreview = "请先选择文件以查看预览";

    [ObservableProperty]
    private string progressMessage = "等待打印";

    [ObservableProperty]
    private bool isBusy;

    /// <summary>
    /// 当前是否存在可视化标签预览模板。
    /// </summary>
    public bool HasTemplatePreview => FilePreviewTemplate is not null;

    partial void OnFilePreviewTemplateChanged(LabelTemplateDocument? value)
    {
        OnPropertyChanged(nameof(HasTemplatePreview));
    }

    /// <summary>
    /// 选择要直接打印的文件。
    /// </summary>
    [RelayCommand]
    private async Task BrowseFileAsync()
    {
        if (_fileDialogService.TryOpenFile("支持文件|*.sclabel;*.prn;*.bin;*.txt|所有文件|*.*", out var path))
        {
            FilePath = path;
            PayloadSummary = $"待打印文件: {Path.GetFileName(path)}";
            await UpdateFilePreviewAsync(path);
            _statusCenter.SetActivityMessage($"已选择文件 {Path.GetFileName(path)}");
        }
    }

    /// <summary>
    /// 更新选中文件的预览内容。
    /// </summary>
    private async Task UpdateFilePreviewAsync(string path)
    {
        try
        {
            var extension = Path.GetExtension(path).ToLowerInvariant();
            FilePreviewTemplate = null;

            if (extension == ".sclabel")
            {
                var template = await _labelTemplateStorageService.LoadAsync(path);
                FilePreviewTemplate = template;
                FilePreview = "已解析标签模板，图形预览已加载。";
            }
            else if (extension == ".txt")
            {
                var text = await DecodeTextFileAsync(path);
                if (_tsplParser.TryParse(text, out var template))
                {
                    FilePreviewTemplate = template;
                    FilePreview = "已解析 TSPL 文档并显示图形预览。";
                }
                else
                {
                    const int maxLength = 3000;
                    FilePreview = text.Length > maxLength ? text[..maxLength] + "\r\n...（已截断）" : text;
                }
            }
            else if (extension is ".prn" or ".bin")
            {
                var bytes = await File.ReadAllBytesAsync(path);
                FilePreview = $"二进制文件: {bytes.Length} 字节\r\n" + string.Join(" ", bytes.Take(128).Select(b => b.ToString("X2")));
            }
            else
            {
                FilePreview = "无法预览此类型文件。";
            }
        }
        catch (Exception ex)
        {
            FilePreviewTemplate = null;
            FilePreview = $"预览失败: {ex.Message}";
        }
    }

    private static async Task<string> DecodeTextFileAsync(string path)
    {
        var rawBytes = await File.ReadAllBytesAsync(path).ConfigureAwait(false);
        return TsplTextDecoder.DecodeTextFileBytes(rawBytes);
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
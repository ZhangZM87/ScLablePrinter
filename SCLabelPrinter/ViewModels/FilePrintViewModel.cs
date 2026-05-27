using System.IO;
using System.Linq;
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
    private readonly ITsplInputAnalyzer _inputAnalyzer;
    private readonly ITsplParser _tsplParser;
    private readonly StatusCenter _statusCenter;

    /// <summary>
    /// 创建文件打印视图模型。
    /// </summary>
    public FilePrintViewModel(IPrintFileService printFileService, IPrinterService printerService, IFileDialogService fileDialogService, IUserNotificationService notificationService, ILabelTemplateStorageService labelTemplateStorageService, ITsplInputAnalyzer inputAnalyzer, ITsplParser tsplParser, StatusCenter statusCenter)
    {
        _printFileService = printFileService;
        _printerService = printerService;
        _fileDialogService = fileDialogService;
        _notificationService = notificationService;
        _labelTemplateStorageService = labelTemplateStorageService;
        _inputAnalyzer = inputAnalyzer;
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
            else if (extension is ".txt" or ".prn" or ".bin")
            {
                var rawBytes = await File.ReadAllBytesAsync(path);
                var analysis = _inputAnalyzer.Analyze(rawBytes);
                UpdatePreviewFromAnalysis(analysis);
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

    /// <summary>
    /// 根据统一分析结果刷新文件预览内容与图形模板。
    /// </summary>
    private void UpdatePreviewFromAnalysis(PrintInputAnalysis analysis)
    {
        if (analysis.Kind == PrintInputKind.TsplCommands && _tsplParser.TryParse(analysis.DecodedText, out var template))
        {
            FilePreviewTemplate = template;
            FilePreview = analysis.IsHexDump
                ? "已识别十六进制 TSPL 并显示图形预览。"
                : "已解析 TSPL 文档并显示图形预览。";
            return;
        }

        FilePreviewTemplate = null;
        FilePreview = analysis.Kind switch
        {
            PrintInputKind.Binary => BuildBinaryPreviewSummary(analysis.PayloadBytes, analysis.IsHexDump),
            _ => BuildTextPreviewSummary(analysis.DecodedText),
        };
    }

    /// <summary>
    /// 为不可视化的二进制内容生成清晰的摘要说明，避免错误渲染成乱码预览。
    /// </summary>
    private static string BuildBinaryPreviewSummary(byte[] payloadBytes, bool isHexDump)
    {
        var previewBytes = payloadBytes.Length > 128 ? payloadBytes[..128] : payloadBytes;
        var sourceHint = isHexDump ? "检测到十六进制转储形式的原始打印包，当前不生成图形预览。" : "检测到原始二进制打印包，当前不生成图形预览。";
        return $"{sourceHint}\r\n文件大小: {payloadBytes.Length} 字节\r\n摘要: {string.Join(" ", previewBytes.Select(b => b.ToString("X2")))}";
    }

    /// <summary>
    /// 对普通文本预览内容进行长度裁剪，避免大文件撑爆摘要区。
    /// </summary>
    private static string BuildTextPreviewSummary(string text)
    {
        const int maxLength = 3000;
        return text.Length > maxLength ? text[..maxLength] + "\r\n...（已截断）" : text;
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
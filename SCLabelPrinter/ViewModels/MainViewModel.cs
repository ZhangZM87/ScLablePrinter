using CommunityToolkit.Mvvm.ComponentModel;
using SCLabelPrinter.Services;

namespace SCLabelPrinter.ViewModels;

/// <summary>
/// 聚合主窗口所需的页面视图模型和状态中心。
/// </summary>
public sealed class MainViewModel : ObservableObject
{
    /// <summary>
    /// 创建主窗口视图模型。
    /// </summary>
    public MainViewModel(PrinterViewModel printerViewModel, EditorViewModel editorViewModel, FilePrintViewModel filePrintViewModel, StatusCenter statusCenter)
    {
        Printer = printerViewModel;
        Editor = editorViewModel;
        FilePrint = filePrintViewModel;
        Status = statusCenter;
    }

    public PrinterViewModel Printer { get; }

    public EditorViewModel Editor { get; }

    public FilePrintViewModel FilePrint { get; }

    public StatusCenter Status { get; }
}
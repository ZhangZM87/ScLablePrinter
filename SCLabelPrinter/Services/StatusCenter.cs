using CommunityToolkit.Mvvm.ComponentModel;

namespace SCLabelPrinter.Services;

/// <summary>
/// 提供跨页面共享的状态栏消息中心。
/// </summary>
public partial class StatusCenter : ObservableObject
{
    [ObservableProperty]
    private string printerMessage = "打印机: 未连接";

    [ObservableProperty]
    private string documentMessage = "标签: 60x40 mm";

    [ObservableProperty]
    private string activityMessage = "系统就绪";

    /// <summary>
    /// 更新打印机状态栏文本。
    /// </summary>
    public void SetPrinterMessage(string message)
    {
        PrinterMessage = message;
    }

    /// <summary>
    /// 更新文档状态栏文本。
    /// </summary>
    public void SetDocumentMessage(string message)
    {
        DocumentMessage = message;
    }

    /// <summary>
    /// 更新活动状态栏文本。
    /// </summary>
    public void SetActivityMessage(string message)
    {
        ActivityMessage = message;
    }
}
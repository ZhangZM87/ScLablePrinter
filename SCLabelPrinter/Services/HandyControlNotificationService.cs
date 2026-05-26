using HandyControl.Controls;

namespace SCLabelPrinter.Services;

/// <summary>
/// 基于 HandyControl Growl 的用户通知服务实现。
/// </summary>
public sealed class HandyControlNotificationService : IUserNotificationService
{
    public void ShowSuccess(string message)
    {
        Growl.Success(message);
    }

    public void ShowInfo(string message)
    {
        Growl.Info(message);
    }

    public void ShowWarning(string message)
    {
        Growl.Warning(message);
    }

    public void ShowError(string message)
    {
        Growl.Error(message);
    }
}

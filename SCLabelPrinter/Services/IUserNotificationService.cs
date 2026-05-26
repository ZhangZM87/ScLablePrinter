namespace SCLabelPrinter.Services;

/// <summary>
/// 定义应用内部的用户通知接口，支持成功、信息、警告和错误提示。
/// </summary>
public interface IUserNotificationService
{
    void ShowSuccess(string message);

    void ShowInfo(string message);

    void ShowWarning(string message);

    void ShowError(string message);
}

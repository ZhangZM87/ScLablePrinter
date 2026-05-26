namespace SCLabelPrinter.Services;

/// <summary>
/// 定义统一的文件选择和保存对话框接口，隔离 ViewModel 对 WPF 对话框的直接依赖。
/// </summary>
public interface IFileDialogService
{
    /// <summary>
    /// 打开文件选择对话框并返回用户选中的路径。
    /// </summary>
    bool TryOpenFile(string filter, out string path);

    /// <summary>
    /// 打开保存文件对话框并返回用户选择的保存路径。
    /// </summary>
    bool TrySaveFile(string filter, string? defaultFileName, out string path);
}
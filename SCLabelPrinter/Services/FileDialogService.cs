using Microsoft.Win32;

namespace SCLabelPrinter.Services;

/// <summary>
/// 提供基于 WPF 标准对话框的文件选择服务实现。
/// </summary>
public sealed class FileDialogService : IFileDialogService
{
    /// <summary>
    /// 显示打开文件对话框并返回结果。
    /// </summary>
    public bool TryOpenFile(string filter, out string path)
    {
        var dialog = new OpenFileDialog
        {
            Filter = filter,
            CheckFileExists = true,
            Multiselect = false,
        };

        var result = dialog.ShowDialog();
        path = result == true ? dialog.FileName ?? string.Empty : string.Empty;
        return result == true;
    }

    /// <summary>
    /// 显示保存文件对话框并返回结果。
    /// </summary>
    public bool TrySaveFile(string filter, string? defaultFileName, out string path)
    {
        var dialog = new SaveFileDialog
        {
            Filter = filter,
            FileName = defaultFileName,
            AddExtension = true,
            OverwritePrompt = true,
        };

        var result = dialog.ShowDialog();
        path = result == true ? dialog.FileName ?? string.Empty : string.Empty;
        return result == true;
    }
}
using SCLabelPrinter.Core.Models;

namespace SCLabelPrinter.Core.Storage;

/// <summary>
/// 定义标签模板文件的读取和保存接口。
/// </summary>
public interface ILabelTemplateStorageService
{
    /// <summary>
    /// 从指定路径加载标签模板。
    /// </summary>
    Task<LabelTemplateDocument> LoadAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// 将标签模板保存到指定路径。
    /// </summary>
    Task SaveAsync(string path, LabelTemplateDocument template, CancellationToken cancellationToken = default);
}
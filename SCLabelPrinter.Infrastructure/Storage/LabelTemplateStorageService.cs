using SCLabelPrinter.Core.Models;
using SCLabelPrinter.Core.Serialization;
using SCLabelPrinter.Core.Storage;

namespace SCLabelPrinter.Infrastructure.Storage;

/// <summary>
/// 提供基于本地文件系统的标签模板存取实现。
/// </summary>
public sealed class LabelTemplateStorageService : ILabelTemplateStorageService
{
    private readonly LabelTemplateSerializer _serializer;

    /// <summary>
    /// 创建标签模板存储服务。
    /// </summary>
    public LabelTemplateStorageService(LabelTemplateSerializer serializer)
    {
        _serializer = serializer;
    }

    /// <summary>
    /// 从文件系统加载标签模板。
    /// </summary>
    public async Task<LabelTemplateDocument> LoadAsync(string path, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var json = await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
        return _serializer.Deserialize(json);
    }

    /// <summary>
    /// 将标签模板保存到文件系统。
    /// </summary>
    public async Task SaveAsync(string path, LabelTemplateDocument template, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(template);

        var json = _serializer.Serialize(template);
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await File.WriteAllTextAsync(path, json, cancellationToken).ConfigureAwait(false);
    }
}
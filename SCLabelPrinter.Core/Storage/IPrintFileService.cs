namespace SCLabelPrinter.Core.Storage;

/// <summary>
/// 定义将外部文件转换为可直接发送到打印机的数据载荷的接口。
/// </summary>
public interface IPrintFileService
{
    /// <summary>
    /// 根据文件类型加载并生成可打印的数据载荷。
    /// </summary>
    Task<PrintPayload> LoadPayloadAsync(string path, int copies, CancellationToken cancellationToken = default);
}

/// <summary>
/// 表示一次打印文件解析后的二进制数据载荷。
/// </summary>
public sealed class PrintPayload
{
    public string SourcePath { get; init; } = string.Empty;

    public string ContentType { get; init; } = string.Empty;

    public byte[] Data { get; init; } = [];
}
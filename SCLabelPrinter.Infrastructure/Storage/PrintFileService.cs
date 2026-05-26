using System.Text;
using SCLabelPrinter.Core.Printing;
using SCLabelPrinter.Core.Storage;

namespace SCLabelPrinter.Infrastructure.Storage;

/// <summary>
/// 提供按文件类型生成打印数据载荷的实现。
/// </summary>
public sealed class PrintFileService : IPrintFileService
{
    private readonly ILabelTemplateStorageService _templateStorageService;
    private readonly TsplGenerator _tsplGenerator;

    /// <summary>
    /// 创建打印文件服务。
    /// </summary>
    public PrintFileService(ILabelTemplateStorageService templateStorageService, TsplGenerator tsplGenerator)
    {
        _templateStorageService = templateStorageService;
        _tsplGenerator = tsplGenerator;
    }

    /// <summary>
    /// 按文件后缀读取内容并生成统一的打印数据载荷。
    /// </summary>
    public async Task<PrintPayload> LoadPayloadAsync(string path, int copies, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var extension = Path.GetExtension(path).ToLowerInvariant();
        return extension switch
        {
            ".sclabel" => await LoadTemplatePayloadAsync(path, copies, cancellationToken).ConfigureAwait(false),
            ".txt" => await LoadTextPayloadAsync(path, cancellationToken).ConfigureAwait(false),
            ".prn" or ".bin" => await LoadBinaryPayloadAsync(path, cancellationToken).ConfigureAwait(false),
            _ => throw new NotSupportedException($"暂不支持的文件类型: {extension}"),
        };
    }

    /// <summary>
    /// 读取模板文件并生成 TSPL 数据载荷。
    /// </summary>
    private async Task<PrintPayload> LoadTemplatePayloadAsync(string path, int copies, CancellationToken cancellationToken)
    {
        var template = await _templateStorageService.LoadAsync(path, cancellationToken).ConfigureAwait(false);
        var command = _tsplGenerator.Generate(template, copies);
        return new PrintPayload
        {
            SourcePath = path,
            ContentType = "template",
            Data = Encoding.GetEncoding(54936).GetBytes(command),
        };
    }

    /// <summary>
    /// 读取 TSPL 文本文件并转换为 GB18030 编码的数据载荷。
    /// </summary>
    private static async Task<PrintPayload> LoadTextPayloadAsync(string path, CancellationToken cancellationToken)
    {
        var rawBytes = await File.ReadAllBytesAsync(path, cancellationToken).ConfigureAwait(false);
        var rawText = Encoding.GetEncoding(54936).GetString(rawBytes);
        if (TsplTextDecoder.TryDecodeHexDump(rawText, out var decodedBytes))
        {
            return new PrintPayload
            {
                SourcePath = path,
                ContentType = "text-hex",
                Data = decodedBytes,
            };
        }

        return new PrintPayload
        {
            SourcePath = path,
            ContentType = "text",
            Data = Encoding.GetEncoding(54936).GetBytes(rawText),
        };
    }

    /// <summary>
    /// 读取二进制打印文件并保持原始数据不变。
    /// </summary>
    private static async Task<PrintPayload> LoadBinaryPayloadAsync(string path, CancellationToken cancellationToken)
    {
        return new PrintPayload
        {
            SourcePath = path,
            ContentType = "binary",
            Data = await File.ReadAllBytesAsync(path, cancellationToken).ConfigureAwait(false),
        };
    }
}
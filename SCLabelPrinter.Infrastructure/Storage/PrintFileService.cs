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
    private readonly ITsplInputAnalyzer _inputAnalyzer;

    /// <summary>
    /// 创建打印文件服务。
    /// </summary>
    public PrintFileService(ILabelTemplateStorageService templateStorageService, TsplGenerator tsplGenerator, ITsplInputAnalyzer inputAnalyzer)
    {
        _templateStorageService = templateStorageService;
        _tsplGenerator = tsplGenerator;
        _inputAnalyzer = inputAnalyzer;
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
    /// 读取文本文件并根据内容特征决定发送文本指令、十六进制解码内容或原始二进制。
    /// </summary>
    private async Task<PrintPayload> LoadTextPayloadAsync(string path, CancellationToken cancellationToken)
    {
        var rawBytes = await File.ReadAllBytesAsync(path, cancellationToken).ConfigureAwait(false);
        var analysis = _inputAnalyzer.Analyze(rawBytes);
        var contentType = analysis.Kind == PrintInputKind.Binary
            ? "binary"
            : analysis.IsHexDump
                ? "text-hex"
                : "text";

        return new PrintPayload
        {
            SourcePath = path,
            ContentType = contentType,
            Data = analysis.PayloadBytes,
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
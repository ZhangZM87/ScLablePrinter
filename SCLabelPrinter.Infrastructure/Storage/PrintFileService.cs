using System.Text;
using SCLabelPrinter.Core.Printing;
using SCLabelPrinter.Core.Storage;

namespace SCLabelPrinter.Infrastructure.Storage;

/// <summary>
/// 鎻愪緵鎸夋枃浠剁被鍨嬬敓鎴愭墦鍗版暟鎹�杞借嵎鐨勫疄鐜般��
/// </summary>
public sealed class PrintFileService : IPrintFileService
{
    private readonly ILabelTemplateStorageService _templateStorageService;
    private readonly TsplGenerator _tsplGenerator;
    private readonly ITsplInputAnalyzer _inputAnalyzer;

    /// <summary>
    /// 鍒涘缓鎵撳嵃鏂囦欢鏈嶅姟銆�
    /// </summary>
    public PrintFileService(ILabelTemplateStorageService templateStorageService, TsplGenerator tsplGenerator, ITsplInputAnalyzer inputAnalyzer)
    {
        _templateStorageService = templateStorageService;
        _tsplGenerator = tsplGenerator;
        _inputAnalyzer = inputAnalyzer;
    }

    /// <summary>
    /// 鎸夋枃浠跺悗缂�璇诲彇鍐呭�瑰苟鐢熸垚缁熶竴鐨勬墦鍗版暟鎹�杞借嵎銆�
    /// </summary>
    public async Task<PrintPayload> LoadPayloadAsync(string path, int copies, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var extension = Path.GetExtension(path).ToLowerInvariant();
        return extension switch
        {
            ".sclabel" => await LoadTemplatePayloadAsync(path, copies, cancellationToken).ConfigureAwait(false),
            ".tspl" => await LoadTsplPayloadAsync(path, cancellationToken).ConfigureAwait(false),
            ".txt" => await LoadTextPayloadAsync(path, cancellationToken).ConfigureAwait(false),
            ".prn" or ".bin" => await LoadBinaryPayloadAsync(path, cancellationToken).ConfigureAwait(false),
            _ => throw new NotSupportedException($"鏆備笉鏀�鎸佺殑鏂囦欢绫诲瀷: {extension}"),
        };
    }

    /// <summary>
    /// 璇诲彇妯℃澘鏂囦欢骞剁敓鎴� TSPL 鏁版嵁杞借嵎銆�
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
    /// 璇诲彇 .tspl 鏂囦欢锛堝凡鍚�瀹屾暣 TSPL 鎸囦护锛夛紝杞�涓烘墦鍗版満鎵�闇�鐨� GB18030 瀛楄妭娴併��
    /// 鏀�鎸� UTF-8 BOM 鍜屾棫鐗� GB2312 缂栫爜鏂囦欢銆�
    /// </summary>
    private static async Task<PrintPayload> LoadTsplPayloadAsync(string path, CancellationToken cancellationToken)
    {
        string tsplText;
        using (var reader = new StreamReader(path, Encoding.GetEncoding("GB2312"), detectEncodingFromByteOrderMarks: true))
        {
            tsplText = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
        }

        return new PrintPayload
        {
            SourcePath = path,
            ContentType = "tspl",
            Data = Encoding.GetEncoding(54936).GetBytes(tsplText),
        };
    }

    /// <summary>
    /// 璇诲彇鏂囨湰鏂囦欢骞舵牴鎹�鍐呭�圭壒寰佸喅瀹氬彂閫佹枃鏈�鎸囦护銆佸崄鍏�杩涘埗瑙ｇ爜鍐呭�规垨鍘熷�嬩簩杩涘埗銆�
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
    /// 璇诲彇浜岃繘鍒舵墦鍗版枃浠跺苟淇濇寔鍘熷�嬫暟鎹�涓嶅彉銆�
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

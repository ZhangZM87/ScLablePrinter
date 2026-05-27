using System.Text;

namespace SCLabelPrinter.Core.Printing;

/// <summary>
/// 定义打印输入的逻辑类型，供发送链路与预览链路共享判定结果。
/// </summary>
public enum PrintInputKind
{
    TsplCommands,
    PlainText,
    Binary,
}

/// <summary>
/// 表示一次打印输入分析后的统一结果。
/// </summary>
public sealed class PrintInputAnalysis
{
    public PrintInputKind Kind { get; init; }

    public bool IsHexDump { get; init; }

    public string DecodedText { get; init; } = string.Empty;

    public byte[] PayloadBytes { get; init; } = Array.Empty<byte>();

    public bool ShouldRenderGraphicPreview => Kind == PrintInputKind.TsplCommands;
}

/// <summary>
/// 定义对打印输入数据进行分类和解码的抽象接口。
/// </summary>
public interface ITsplInputAnalyzer
{
    /// <summary>
    /// 对原始输入字节进行分类，返回统一的解码与预览判定结果。
    /// </summary>
    PrintInputAnalysis Analyze(byte[] rawBytes);
}

/// <summary>
/// 根据内容特征识别 TSPL 指令文本、普通文本与原始二进制打印包。
/// </summary>
public sealed class TsplInputAnalyzer : ITsplInputAnalyzer
{
    private static readonly Encoding TsplEncoding = CreateTsplEncoding();
    private static readonly HashSet<string> KnownTsplCommands = new(StringComparer.OrdinalIgnoreCase)
    {
        "SIZE",
        "GAP",
        "CLS",
        "PRINT",
        "DENSITY",
        "TEXT",
        "BARCODE",
        "QRCODE",
        "BOX",
        "BAR",
        "ERASE",
        "BITMAP",
        "REFERENCE",
        "DIRECTION",
        "CODEPAGE",
        "OFFSET",
        "HOME",
    };

    /// <summary>
    /// 对原始输入字节进行分类，返回统一的解码与预览判定结果。
    /// </summary>
    public PrintInputAnalysis Analyze(byte[] rawBytes)
    {
        ArgumentNullException.ThrowIfNull(rawBytes);

        if (rawBytes.Length == 0)
        {
            return new PrintInputAnalysis
            {
                Kind = PrintInputKind.PlainText,
                PayloadBytes = Array.Empty<byte>(),
                DecodedText = string.Empty,
            };
        }

        var rawText = TsplEncoding.GetString(rawBytes);
        if (TsplTextDecoder.TryDecodeHexDump(rawText, out var decodedBytes))
        {
            var decodedText = TsplEncoding.GetString(decodedBytes);
            return CreateAnalysis(decodedBytes, decodedText, isHexDump: true);
        }

        return CreateAnalysis(rawBytes, rawText, isHexDump: false);
    }

    /// <summary>
    /// 根据字节载荷和解码文本生成最终分析结果。
    /// </summary>
    private static PrintInputAnalysis CreateAnalysis(byte[] payloadBytes, string decodedText, bool isHexDump)
    {
        if (LooksLikeTsplCommands(decodedText))
        {
            return new PrintInputAnalysis
            {
                Kind = PrintInputKind.TsplCommands,
                IsHexDump = isHexDump,
                PayloadBytes = payloadBytes,
                DecodedText = decodedText,
            };
        }

        if (LooksLikeBinary(payloadBytes))
        {
            return new PrintInputAnalysis
            {
                Kind = PrintInputKind.Binary,
                IsHexDump = isHexDump,
                PayloadBytes = payloadBytes,
                DecodedText = string.Empty,
            };
        }

        return new PrintInputAnalysis
        {
            Kind = PrintInputKind.PlainText,
            IsHexDump = isHexDump,
            PayloadBytes = payloadBytes,
            DecodedText = decodedText,
        };
    }

    /// <summary>
    /// 基于控制字符密度判断输入是否更接近原始二进制打印包。
    /// </summary>
    private static bool LooksLikeBinary(byte[] payloadBytes)
    {
        if (payloadBytes.Length == 0)
        {
            return false;
        }

        var sampleLength = Math.Min(512, payloadBytes.Length);
        var controlCount = 0;
        for (var index = 0; index < sampleLength; index++)
        {
            var value = payloadBytes[index];
            if (value == 0x00)
            {
                return true;
            }

            var isControl = value < 0x20 && value is not 0x09 and not 0x0A and not 0x0D;
            if (isControl || value == 0x7F)
            {
                controlCount++;
            }
        }

        return (double)controlCount / sampleLength >= 0.12;
    }

    /// <summary>
    /// 通过 TSPL 命令命中数判断文本是否属于可解析的标签指令。
    /// </summary>
    private static bool LooksLikeTsplCommands(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var lines = text
            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim())
            .Where(line => line.Length > 0)
            .Take(32)
            .ToArray();

        if (lines.Length == 0)
        {
            return false;
        }

        var commandHits = 0;
        foreach (var line in lines)
        {
            var separatorIndex = line.IndexOfAny(new[] { ' ', '\t', ',' });
            var token = separatorIndex >= 0 ? line[..separatorIndex] : line;
            if (KnownTsplCommands.Contains(token))
            {
                commandHits++;
            }
        }

        return commandHits >= 2 || (commandHits == 1 && lines.Length == 1);
    }

    /// <summary>
    /// 创建 GB18030 编码实例，以兼容标签打印常用文本数据。
    /// </summary>
    private static Encoding CreateTsplEncoding()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        return Encoding.GetEncoding(54936);
    }
}
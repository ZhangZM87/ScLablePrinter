using System.Globalization;
using System.Text;

namespace SCLabelPrinter.Core.Printing;

/// <summary>
/// 提供 TSPL 文本文件与十六进制 TSPL 转换的辅助方法。
/// </summary>
public static class TsplTextDecoder
{
    private static readonly Encoding TsplEncoding = CreateTsplEncoding();

    private static Encoding CreateTsplEncoding()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        return Encoding.GetEncoding(54936);
    }

    /// <summary>
    /// 从文本文件字节读取并返回可供解析的 TSPL 文本。
    /// </summary>
    public static string DecodeTextFileBytes(byte[] bytes)
    {
        var rawText = TsplEncoding.GetString(bytes);
        return TryDecodeHexDump(rawText, out var decodedBytes)
            ? TsplEncoding.GetString(decodedBytes)
            : rawText;
    }

    /// <summary>
    /// 如果文本是 TSPL 十六进制转储，则解析为实际 TSPL 字节。
    /// </summary>
    public static bool TryDecodeHexDump(string text, out byte[] bytes)
    {
        bytes = Array.Empty<byte>();
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var trimmed = text.Trim();
        if (trimmed.Length == 0)
        {
            return false;
        }

        if (trimmed.All(ch => char.IsWhiteSpace(ch) || IsHexDigit(ch)))
        {
            var cleaned = new string(trimmed.Where(ch => !char.IsWhiteSpace(ch)).ToArray());
            if (cleaned.Length % 2 != 0 || cleaned.Length > 40000)
            {
                return false;
            }

            bytes = new byte[cleaned.Length / 2];
            for (var index = 0; index < bytes.Length; index++)
            {
                var token = cleaned.Substring(index * 2, 2);
                if (!byte.TryParse(token, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var value))
                {
                    return false;
                }

                bytes[index] = value;
            }

            return true;
        }

        var tokens = trimmed.Split((char[])null!, StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length == 0 || tokens.Length > 20000)
        {
            return false;
        }

        var decoded = new byte[tokens.Length];
        for (var index = 0; index < tokens.Length; index++)
        {
            var token = tokens[index].Trim();
            if (token.Length != 2 || !byte.TryParse(token, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var value))
            {
                return false;
            }

            decoded[index] = value;
        }

        bytes = decoded;
        return true;
    }

    private static bool IsHexDigit(char ch)
    {
        return (ch >= '0' && ch <= '9') || (ch >= 'A' && ch <= 'F') || (ch >= 'a' && ch <= 'f');
    }
}

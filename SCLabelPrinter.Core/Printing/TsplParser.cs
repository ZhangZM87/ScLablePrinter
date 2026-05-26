using System.Globalization;
using System.Text;
using SCLabelPrinter.Core.Models;

namespace SCLabelPrinter.Core.Printing;

/// <summary>
/// 将 TSPL 文本解析为标签模板文档，用于文件预览 和 图形渲染。
/// </summary>
public sealed class TsplParser : ITsplParser
{
    /// <summary>
    /// 解析 TSPL 文本为标签模板文档。
    /// </summary>
    public LabelTemplateDocument Parse(string tsplText)
    {
        if (string.IsNullOrWhiteSpace(tsplText))
        {
            throw new ArgumentException("TSPL 文本不能为空。", nameof(tsplText));
        }

        var document = new LabelTemplateDocument();
        document.Label = new LabelDefinition();

        foreach (var rawLine in tsplText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
        {
            var line = rawLine.Trim();
            if (line.Length == 0)
            {
                continue;
            }

            var firstSpace = line.IndexOf(' ');
            var command = firstSpace < 0 ? line.ToUpperInvariant() : line[..firstSpace].ToUpperInvariant();
            var arguments = firstSpace < 0 ? string.Empty : line[(firstSpace + 1)..].Trim();

            switch (command)
            {
                case "SIZE":
                    ParseSize(document.Label, arguments);
                    break;
                case "GAP":
                    ParseGap(document.Label, arguments);
                    break;
                case "DENSITY":
                    document.Label.Density = ParseInt(arguments, document.Label.Density);
                    break;
                case "BOX":
                    ParseBox(document, arguments);
                    break;
                case "BAR":
                    ParseBar(document, arguments);
                    break;
                case "ERASE":
                    ParseErase(document, arguments);
                    break;
                case "TEXT":
                    ParseText(document, arguments);
                    break;
                case "BARCODE":
                    ParseBarcode(document, arguments);
                    break;
                case "QRCODE":
                    ParseQrCode(document, arguments);
                    break;
                case "CLS":
                case "PRINT":
                    break;
                default:
                    break;
            }
        }

        return document;
    }

    /// <summary>
    /// 尝试解析 TSPL 文本为标签模板文档。
    /// </summary>
    public bool TryParse(string tsplText, out LabelTemplateDocument? template)
    {
        template = null;
        try
        {
            template = Parse(tsplText);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static void ParseSize(LabelDefinition label, string arguments)
    {
        var args = SplitArguments(arguments);
        if (args.Length < 2)
        {
            return;
        }

        if (TryParseLength(args[0], out var width, out var unit))
        {
            label.Width = (int)Math.Round(width);
            label.Unit = unit;
        }

        if (TryParseLength(args[1], out var height, out _))
        {
            label.Height = (int)Math.Round(height);
        }
    }

    private static void ParseGap(LabelDefinition label, string arguments)
    {
        var args = SplitArguments(arguments);
        if (args.Length == 0)
        {
            return;
        }

        if (TryParseLength(args[0], out var gap, out _))
        {
            label.Gap = (int)Math.Round(gap);
        }
    }

    private static void ParseBox(LabelTemplateDocument document, string arguments)
    {
        var args = SplitArguments(arguments);
        if (args.Length < 5)
        {
            return;
        }

        document.Elements.Add(new BoxElement
        {
            X = ParseInt(args[0]),
            Y = ParseInt(args[1]),
            EndX = ParseInt(args[2]),
            EndY = ParseInt(args[3]),
            Thickness = ParseInt(args[4], 2),
        });
    }

    private static void ParseBar(LabelTemplateDocument document, string arguments)
    {
        var args = SplitArguments(arguments);
        if (args.Length < 4)
        {
            return;
        }

        document.Elements.Add(new LineElement
        {
            X = ParseInt(args[0]),
            Y = ParseInt(args[1]),
            Width = ParseInt(args[2]),
            Height = ParseInt(args[3]),
        });
    }

    private static void ParseErase(LabelTemplateDocument document, string arguments)
    {
        var args = SplitArguments(arguments);
        if (args.Length < 4)
        {
            return;
        }

        document.Elements.Add(new EraseElement
        {
            X = ParseInt(args[0]),
            Y = ParseInt(args[1]),
            Width = ParseInt(args[2]),
            Height = ParseInt(args[3]),
        });
    }

    private static void ParseText(LabelTemplateDocument document, string arguments)
    {
        var args = SplitArguments(arguments);
        if (args.Length < 7)
        {
            return;
        }

        document.Elements.Add(new TextElement
        {
            X = ParseInt(args[0]),
            Y = ParseInt(args[1]),
            Font = TrimQuotes(args[2]),
            Rotation = ParseInt(args[3]),
            XScale = ParseInt(args[4], 1),
            YScale = ParseInt(args[5], 1),
            Content = TrimQuotes(args[6]),
        });
    }

    private static void ParseBarcode(LabelTemplateDocument document, string arguments)
    {
        var args = SplitArguments(arguments);
        if (args.Length < 9)
        {
            return;
        }

        document.Elements.Add(new BarcodeElement
        {
            X = ParseInt(args[0]),
            Y = ParseInt(args[1]),
            CodeType = MapBarcodeType(TrimQuotes(args[2])),
            Height = ParseInt(args[3], 80),
            Readable = ParseInt(args[4], 0) != 0,
            Rotation = ParseInt(args[5]),
            Narrow = ParseInt(args[6], 2),
            Wide = ParseInt(args[7], 2),
            Content = TrimQuotes(args[8]),
        });
    }

    private static void ParseQrCode(LabelTemplateDocument document, string arguments)
    {
        var args = SplitArguments(arguments);
        if (args.Length < 7)
        {
            return;
        }

        document.Elements.Add(new QrCodeElement
        {
            X = ParseInt(args[0]),
            Y = ParseInt(args[1]),
            ErrorCorrectionLevel = TrimQuotes(args[2]),
            CellWidth = ParseInt(args[3], 5),
            Mode = TrimQuotes(args[4]),
            Rotation = ParseInt(args[5]),
            Content = TrimQuotes(args[6]),
        });
    }

    private static string[] SplitArguments(string text)
    {
        var items = new List<string>();
        var builder = new StringBuilder();
        var inQuote = false;

        foreach (var ch in text)
        {
            if (ch == '"')
            {
                inQuote = !inQuote;
                builder.Append(ch);
                continue;
            }

            if (ch == ',' && !inQuote)
            {
                items.Add(builder.ToString().Trim());
                builder.Clear();
                continue;
            }

            builder.Append(ch);
        }

        if (builder.Length > 0)
        {
            items.Add(builder.ToString().Trim());
        }

        return items.Where(item => item.Length > 0).ToArray();
    }

    private static bool TryParseLength(string input, out double value, out LabelUnit unit)
    {
        value = 0;
        unit = LabelUnit.Dot;
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        var trimmed = input.Trim();
        if (trimmed.EndsWith("mm", StringComparison.OrdinalIgnoreCase))
        {
            unit = LabelUnit.Millimeter;
            trimmed = trimmed[..^2].Trim();
        }
        else if (trimmed.EndsWith("dot", StringComparison.OrdinalIgnoreCase))
        {
            unit = LabelUnit.Dot;
            trimmed = trimmed[..^3].Trim();
        }

        return double.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }

    private static int ParseInt(string token, int defaultValue = 0)
    {
        return int.TryParse(token.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            ? value
            : defaultValue;
    }

    private static BarcodeType MapBarcodeType(string type)
    {
        return type.Trim().Trim('"').ToUpperInvariant() switch
        {
            "39" => BarcodeType.Code39,
            "128" => BarcodeType.Code128,
            "EAN13" or "EAN-13" => BarcodeType.Ean13,
            _ => BarcodeType.Code128,
        };
    }

    private static string TrimQuotes(string token)
    {
        var value = token.Trim();
        if (value.Length >= 2 && value[0] == '"' && value[^1] == '"')
        {
            return value[1..^1];
        }

        return value;
    }
}

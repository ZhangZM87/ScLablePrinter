using SCLabelPrinter.Core.Models;
using SCLabelPrinter.Core.Printing;

namespace SCLabelPrinter.Core.Export.Tspl;

/// <summary>
/// TSPL 文本元素写入器。
/// </summary>
public sealed class TsplTextElementWriter : IElementWriter
{
    public PrinterLanguage Language => PrinterLanguage.Tspl;

    public bool CanWrite(LabelElement element) => element is TextElement;

    public void Write(LabelElement element, ICommandBuilder builder)
    {
        var text = (TextElement)element;
        builder.AppendLine($"TEXT {text.X},{text.Y},\"{TsplValueFormatter.Escape(text.Font)}\",{text.Rotation},{text.XScale},{text.YScale},\"{TsplValueFormatter.Escape(text.Content)}\"");
    }
}

/// <summary>
/// TSPL 条码元素写入器。
/// </summary>
public sealed class TsplBarcodeElementWriter : IElementWriter
{
    public PrinterLanguage Language => PrinterLanguage.Tspl;

    public bool CanWrite(LabelElement element) => element is BarcodeElement;

    public void Write(LabelElement element, ICommandBuilder builder)
    {
        var barcode = (BarcodeElement)element;
        builder.AppendLine($"BARCODE {barcode.X},{barcode.Y},\"{TsplValueFormatter.MapBarcodeType(barcode.CodeType)}\",{barcode.Height},{(barcode.Readable ? 1 : 0)},{barcode.Rotation},{barcode.Narrow},{barcode.Wide},\"{TsplValueFormatter.Escape(barcode.Content)}\"");
    }
}

/// <summary>
/// TSPL 二维码元素写入器。
/// </summary>
public sealed class TsplQrCodeElementWriter : IElementWriter
{
    public PrinterLanguage Language => PrinterLanguage.Tspl;

    public bool CanWrite(LabelElement element) => element is QrCodeElement;

    public void Write(LabelElement element, ICommandBuilder builder)
    {
        var qr = (QrCodeElement)element;
        builder.AppendLine($"QRCODE {qr.X},{qr.Y},{qr.ErrorCorrectionLevel},{qr.CellWidth},{qr.Mode},{qr.Rotation},\"{TsplValueFormatter.Escape(qr.Content)}\"");
    }
}

/// <summary>
/// TSPL 矩形框元素写入器。
/// </summary>
public sealed class TsplBoxElementWriter : IElementWriter
{
    public PrinterLanguage Language => PrinterLanguage.Tspl;

    public bool CanWrite(LabelElement element) => element is BoxElement;

    public void Write(LabelElement element, ICommandBuilder builder)
    {
        var box = (BoxElement)element;
        builder.AppendLine($"BOX {box.X},{box.Y},{box.EndX},{box.EndY},{box.Thickness}");
    }
}

/// <summary>
/// TSPL 挖空区域元素写入器。
/// </summary>
public sealed class TsplEraseElementWriter : IElementWriter
{
    public PrinterLanguage Language => PrinterLanguage.Tspl;

    public bool CanWrite(LabelElement element) => element is EraseElement;

    public void Write(LabelElement element, ICommandBuilder builder)
    {
        var erase = (EraseElement)element;
        builder.AppendLine($"ERASE {erase.X},{erase.Y},{erase.Width},{erase.Height}");
    }
}
/// <summary>
/// TSPL 线条元素写入器，支持实线和虚线。
/// 虚线通过分段 BAR 指令模拟实现（TSPL 无原生虚线支持）。
/// </summary>
public sealed class TsplLineElementWriter : IElementWriter
{
    public PrinterLanguage Language => PrinterLanguage.Tspl;

    public bool CanWrite(LabelElement element) => element is LineElement;

    public void Write(LabelElement element, ICommandBuilder builder)
    {
        var line = (LineElement)element;

        if (line.Style == TableLineStyle.Solid)
        {
            builder.AppendLine($"BAR {line.X},{line.Y},{line.Width},{line.Height}");
            return;
        }

        var isHorizontal = line.Width >= line.Height;

        if (isHorizontal)
        {
            var segments = DashSegmentCalculator.CalculateHorizontalSegments(
                line.X, line.Y, line.Width, line.Height,
                line.Style, line.DashLength, line.GapLength);

            foreach (var seg in segments)
            {
                builder.AppendLine($"BAR {seg.X},{seg.Y},{seg.Width},{seg.Height}");
            }
        }
        else
        {
            var segments = DashSegmentCalculator.CalculateVerticalSegments(
                line.X, line.Y, line.Height, line.Width,
                line.Style, line.DashLength, line.GapLength);

            foreach (var seg in segments)
            {
                builder.AppendLine($"BAR {seg.X},{seg.Y},{seg.Width},{seg.Height}");
            }
        }
    }
}

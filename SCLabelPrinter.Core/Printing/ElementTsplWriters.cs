using System.Collections.Generic;
using System.Globalization;
using SCLabelPrinter.Core.Models;

namespace SCLabelPrinter.Core.Printing;

/// <summary>
/// 提供文本元素的 TSPL 输出实现。
/// </summary>
public sealed class TextElementTsplWriter : IElementTsplWriter
{
    /// <summary>
    /// 判断当前写入器是否支持文本元素。
    /// </summary>
    public bool CanWrite(LabelElement element)
    {
        return element is TextElement;
    }

    /// <summary>
    /// 将文本元素转换为 TSPL TEXT 指令。
    /// </summary>
    public string Write(LabelElement element)
    {
        var text = (TextElement)element;
        return $"TEXT {text.X},{text.Y},\"{TsplValueFormatter.Escape(text.Font)}\",{text.Rotation},{text.XScale},{text.YScale},\"{TsplValueFormatter.Escape(text.Content)}\"";
    }
}

/// <summary>
/// 提供条码元素的 TSPL 输出实现。
/// </summary>
public sealed class BarcodeElementTsplWriter : IElementTsplWriter
{
    /// <summary>
    /// 判断当前写入器是否支持条码元素。
    /// </summary>
    public bool CanWrite(LabelElement element)
    {
        return element is BarcodeElement;
    }

    /// <summary>
    /// 将条码元素转换为 TSPL BARCODE 指令。
    /// </summary>
    public string Write(LabelElement element)
    {
        var barcode = (BarcodeElement)element;
        return $"BARCODE {barcode.X},{barcode.Y},\"{TsplValueFormatter.MapBarcodeType(barcode.CodeType)}\",{barcode.Height},{(barcode.Readable ? 1 : 0)},{barcode.Rotation},{barcode.Narrow},{barcode.Wide},\"{TsplValueFormatter.Escape(barcode.Content)}\"";
    }
}

/// <summary>
/// 提供二维码元素的 TSPL 输出实现。
/// </summary>
public sealed class QrCodeElementTsplWriter : IElementTsplWriter
{
    /// <summary>
    /// 判断当前写入器是否支持二维码元素。
    /// </summary>
    public bool CanWrite(LabelElement element)
    {
        return element is QrCodeElement;
    }

    /// <summary>
    /// 将二维码元素转换为 TSPL QRCODE 指令。
    /// </summary>
    public string Write(LabelElement element)
    {
        var qrCode = (QrCodeElement)element;
        return $"QRCODE {qrCode.X},{qrCode.Y},{qrCode.ErrorCorrectionLevel},{qrCode.CellWidth},{qrCode.Mode},{qrCode.Rotation},\"{TsplValueFormatter.Escape(qrCode.Content)}\"";
    }
}

/// <summary>
/// 提供矩形框元素的 TSPL 输出实现。
/// </summary>
public sealed class BoxElementTsplWriter : IElementTsplWriter
{
    /// <summary>
    /// 判断当前写入器是否支持矩形框元素。
    /// </summary>
    public bool CanWrite(LabelElement element)
    {
        return element is BoxElement;
    }

    /// <summary>
    /// 将矩形框元素转换为 TSPL BOX 指令。
    /// </summary>
    public string Write(LabelElement element)
    {
        var box = (BoxElement)element;
        return $"BOX {box.X},{box.Y},{box.EndX},{box.EndY},{box.Thickness}";
    }
}

/// <summary>
/// 提供表格元素的 TSPL 输出实现。
/// </summary>
public sealed class TableElementTsplWriter : IElementTsplWriter
{
    /// <summary>
    /// 判断当前写入器是否支持表格元素。
    /// </summary>
    public bool CanWrite(LabelElement element)
    {
        return element is TableElement;
    }

    /// <summary>
    /// 将表格元素转换为 TSPL 指令。
    /// </summary>
    public string Write(LabelElement element)
    {
        var table = (TableElement)element;
        var commands = new List<string>();
        var totalWidth = table.ColumnWidths.Sum();
        var totalHeight = table.TotalHeight;
        commands.Add($"BOX {table.X},{table.Y},{table.X + totalWidth},{table.Y + totalHeight},1");

        var currentX = table.X;
        for (var colIndex = 1; colIndex < table.Cols; colIndex++)
        {
            currentX += table.GetColumnWidth(colIndex - 1);
            commands.Add($"BAR {currentX},{table.Y},1,{totalHeight}");
        }

        for (var row = 1; row < table.Rows; row++)
        {
            var y = table.Y + table.GetRowHeights().Take(row).Sum();
            commands.Add($"BAR {table.X},{y},{totalWidth},1");
        }

        for (var row = 0; row < table.Rows; row++)
        {
            var rowOffsetY = table.Y + row * table.RowHeight + 8;
            var columnOffsetX = table.X;
            for (var col = 0; col < table.Cols; col++)
            {
                var cellIndex = row * table.Cols + col;
                if (cellIndex >= table.Cells.Count)
                {
                    columnOffsetX += table.GetColumnWidth(col);
                    continue;
                }

                var cell = table.Cells[cellIndex];
                var cellX = columnOffsetX + 8;
                var cellY = rowOffsetY;

                if (cell.InnerElements.Count > 0)
                {
                    foreach (var inner in cell.InnerElements)
                    {
                        var innerX = cellX + inner.X;
                        var innerY = cellY + inner.Y;

                        switch (inner)
                        {
                            case TableCellTextElement textElement:
                                commands.Add($"TEXT {innerX},{innerY},\"3\",0,1,1,\"{TsplValueFormatter.Escape(textElement.Content)}\"");
                                break;
                            case TableCellBarcodeElement barcodeElement:
                                commands.Add($"BARCODE {innerX},{innerY},\"{TsplValueFormatter.MapBarcodeType(barcodeElement.BarcodeType)}\",{Math.Max(20, barcodeElement.Height)},{(barcodeElement.Readable ? 1 : 0)},{barcodeElement.Rotation},{barcodeElement.Narrow},{barcodeElement.Wide},\"{TsplValueFormatter.Escape(barcodeElement.Content)}\"");
                                break;
                            case TableCellQrCodeElement qrCodeElement:
                                commands.Add($"QRCODE {innerX},{innerY},{qrCodeElement.ErrorCorrectionLevel},{qrCodeElement.CellWidth},{qrCodeElement.Mode},{qrCodeElement.Rotation},\"{TsplValueFormatter.Escape(qrCodeElement.Content)}\"");
                                break;
                        }
                    }
                }
                else
                {
                    switch (cell.ContentType)
                    {
                        case TableCellContentType.Text:
                            commands.Add($"TEXT {cellX},{cellY},\"3\",0,1,1,\"{TsplValueFormatter.Escape(cell.Content)}\"");
                            break;
                        case TableCellContentType.Barcode:
                            commands.Add($"BARCODE {cellX},{cellY},\"{TsplValueFormatter.MapBarcodeType(cell.BarcodeType)}\",80,1,0,2,2,\"{TsplValueFormatter.Escape(cell.Content)}\"");
                            break;
                        case TableCellContentType.QrCode:
                            commands.Add($"QRCODE {cellX},{cellY},{cell.QrErrorCorrectionLevel},{cell.QrCellWidth},{cell.QrMode},0,\"{TsplValueFormatter.Escape(cell.Content)}\"");
                            break;
                    }
                }

                columnOffsetX += table.GetColumnWidth(col);
            }
        }

        return string.Join("\r\n", commands);
    }
}

/// <summary>
/// 提供线条元素的 TSPL 输出实现。
/// </summary>
public sealed class LineElementTsplWriter : IElementTsplWriter
{
    /// <summary>
    /// 判断当前写入器是否支持线条元素。
    /// </summary>
    public bool CanWrite(LabelElement element)
    {
        return element is LineElement;
    }

    /// <summary>
    /// 将线条元素转换为 TSPL BAR 指令。
    /// </summary>
    public string Write(LabelElement element)
    {
        var line = (LineElement)element;
        return $"BAR {line.X},{line.Y},{line.Width},{line.Height}";
    }
}

/// <summary>
/// 提供挖空区域元素的 TSPL 输出实现。
/// </summary>
public sealed class EraseElementTsplWriter : IElementTsplWriter
{
    /// <summary>
    /// 判断当前写入器是否支持挖空区域元素。
    /// </summary>
    public bool CanWrite(LabelElement element)
    {
        return element is EraseElement;
    }

    /// <summary>
    /// 将挖空区域元素转换为 TSPL ERASE 指令。
    /// </summary>
    public string Write(LabelElement element)
    {
        var erase = (EraseElement)element;
        return $"ERASE {erase.X},{erase.Y},{erase.Width},{erase.Height}";
    }
}

internal static class TsplValueFormatter
{
    /// <summary>
    /// 将标签单位转换为 TSPL 能识别的字符串。
    /// </summary>
    public static string MapUnit(LabelUnit unit)
    {
        return unit == LabelUnit.Dot ? "dot" : "mm";
    }

    /// <summary>
    /// 将条码枚举值转换为 TSPL 约定的条码编码字符串。
    /// </summary>
    public static string MapBarcodeType(BarcodeType barcodeType)
    {
        return barcodeType switch
        {
            BarcodeType.Code39 => "39",
            BarcodeType.Code128 => "128",
            BarcodeType.Ean13 => "EAN13",
            _ => throw new ArgumentOutOfRangeException(nameof(barcodeType), barcodeType, "不支持的条码类型。"),
        };
    }

    /// <summary>
    /// 将数值格式化为适合 TSPL 指令的字符串。
    /// </summary>
    public static string FormatNumber(double value)
    {
        return value.ToString("0.##", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// 转义 TSPL 文本参数中的双引号，避免破坏指令结构。
    /// </summary>
    public static string Escape(string value)
    {
        return value.Replace("\"", "\\\"");
    }
}
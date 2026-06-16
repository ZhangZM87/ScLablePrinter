using SCLabelPrinter.Core.Models;
using SCLabelPrinter.Core.Printing;

namespace SCLabelPrinter.Core.Export.Tspl;

/// <summary>
/// TSPL 表格元素写入器，支持虚线边框和网格线。
/// 根据 TableElement 的 BorderStyle 和 GridStyle 属性决定绘制实线或虚线。
/// </summary>
public sealed class TsplTableElementWriter : IElementWriter
{
    public PrinterLanguage Language => PrinterLanguage.Tspl;

    public bool CanWrite(LabelElement element) => element is TableElement;

    public void Write(LabelElement element, ICommandBuilder builder)
    {
        var table = (TableElement)element;
        var totalWidth = table.ColumnWidths.Sum();
        var totalHeight = table.TotalHeight;

        WriteBorder(table, builder, totalWidth, totalHeight);
        WriteGridLines(table, builder, totalWidth, totalHeight);
        WriteCellContents(table, builder);
    }

    /// <summary>
    /// 绘制表格外边框（支持虚线）。
    /// </summary>
    private static void WriteBorder(TableElement table, ICommandBuilder builder, int totalWidth, int totalHeight)
    {
        var style = table.BorderStyle;
        const int thickness = 1;

        // 上边框
        foreach (var seg in DashSegmentCalculator.CalculateHorizontalSegments(
            table.X, table.Y, totalWidth, thickness, style))
        {
            builder.AppendLine($"BAR {seg.X},{seg.Y},{seg.Width},{seg.Height}");
        }

        // 下边框
        foreach (var seg in DashSegmentCalculator.CalculateHorizontalSegments(
            table.X, table.Y + totalHeight, totalWidth, thickness, style))
        {
            builder.AppendLine($"BAR {seg.X},{seg.Y},{seg.Width},{seg.Height}");
        }

        // 左边框
        foreach (var seg in DashSegmentCalculator.CalculateVerticalSegments(
            table.X, table.Y, totalHeight, thickness, style))
        {
            builder.AppendLine($"BAR {seg.X},{seg.Y},{seg.Width},{seg.Height}");
        }

        // 右边框
        foreach (var seg in DashSegmentCalculator.CalculateVerticalSegments(
            table.X + totalWidth, table.Y, totalHeight, thickness, style))
        {
            builder.AppendLine($"BAR {seg.X},{seg.Y},{seg.Width},{seg.Height}");
        }
    }

    /// <summary>
    /// 绘制表格内部网格线（支持虚线）。
    /// </summary>
    private static void WriteGridLines(TableElement table, ICommandBuilder builder, int totalWidth, int totalHeight)
    {
        var style = table.GridStyle;
        const int thickness = 1;

        // 垂直网格线（列分隔）
        var currentX = table.X;
        for (var col = 0; col < table.Cols - 1; col++)
        {
            currentX += table.GetColumnWidth(col);
            foreach (var seg in DashSegmentCalculator.CalculateVerticalSegments(
                currentX, table.Y, totalHeight, thickness, style))
            {
                builder.AppendLine($"BAR {seg.X},{seg.Y},{seg.Width},{seg.Height}");
            }
        }

        // 水平网格线（行分隔）
        var rowHeights = table.GetRowHeights();
        var currentY = table.Y;
        for (var row = 0; row < table.Rows - 1; row++)
        {
            currentY += rowHeights[row];
            foreach (var seg in DashSegmentCalculator.CalculateHorizontalSegments(
                table.X, currentY, totalWidth, thickness, style))
            {
                builder.AppendLine($"BAR {seg.X},{seg.Y},{seg.Width},{seg.Height}");
            }
        }
    }

    /// <summary>
    /// 绘制单元格内部内容。
    /// </summary>
    private static void WriteCellContents(TableElement table, ICommandBuilder builder)
    {
        var rowHeights = table.GetRowHeights();

        for (var row = 0; row < table.Rows; row++)
        {
            var rowOffsetY = table.Y + rowHeights.Take(row).Sum() + 8;
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
                        WriteInnerElement(inner, innerX, innerY, builder);
                    }
                }
                else
                {
                    WriteLegacyCellContent(cell, cellX, cellY, builder);
                }

                columnOffsetX += table.GetColumnWidth(col);
            }
        }
    }

    /// <summary>
    /// 写入单元格内部元素。
    /// </summary>
    private static void WriteInnerElement(TableCellInnerElement inner, int x, int y, ICommandBuilder builder)
    {
        switch (inner)
        {
            case TableCellTextElement text:
                builder.AppendLine($"TEXT {x},{y},\"{TsplValueFormatter.Escape(text.Font)}\",{text.Rotation},{text.XScale},{text.YScale},\"{TsplValueFormatter.Escape(text.Content)}\"");
                break;
            case TableCellBarcodeElement barcode:
                builder.AppendLine($"BARCODE {x},{y},\"{TsplValueFormatter.MapBarcodeType(barcode.BarcodeType)}\",{Math.Max(20, barcode.Height)},{(barcode.Readable ? 1 : 0)},{barcode.Rotation},{barcode.Narrow},{barcode.Wide},\"{TsplValueFormatter.Escape(barcode.Content)}\"");
                break;
            case TableCellQrCodeElement qr:
                builder.AppendLine($"QRCODE {x},{y},{qr.ErrorCorrectionLevel},{qr.CellWidth},{qr.Mode},{qr.Rotation},\"{TsplValueFormatter.Escape(qr.Content)}\"");
                break;
        }
    }

    /// <summary>
    /// 写入旧版单元格内容（兼容无 InnerElements 的单元格）。
    /// </summary>
    private static void WriteLegacyCellContent(TableCell cell, int x, int y, ICommandBuilder builder)
    {
        switch (cell.ContentType)
        {
            case TableCellContentType.Text:
                builder.AppendLine($"TEXT {x},{y},\"3\",0,1,1,\"{TsplValueFormatter.Escape(cell.Content)}\"");
                break;
            case TableCellContentType.Barcode:
                builder.AppendLine($"BARCODE {x},{y},\"{TsplValueFormatter.MapBarcodeType(cell.BarcodeType)}\",80,1,0,2,2,\"{TsplValueFormatter.Escape(cell.Content)}\"");
                break;
            case TableCellContentType.QrCode:
                builder.AppendLine($"QRCODE {x},{y},{cell.QrErrorCorrectionLevel},{cell.QrCellWidth},{cell.QrMode},0,\"{TsplValueFormatter.Escape(cell.Content)}\"");
                break;
        }
    }
}

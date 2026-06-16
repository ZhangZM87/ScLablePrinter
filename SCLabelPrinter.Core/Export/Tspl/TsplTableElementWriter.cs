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
    /// 绘制表格内部网格线（支持虚线），正确处理合并单元格。
    /// 竖线：仅在确认有 ColSpan 跨越时跳过。
    /// 横线：仅在确认有 RowSpan 跨越时跳过。
    /// </summary>
    private static void WriteGridLines(TableElement table, ICommandBuilder builder, int totalWidth, int totalHeight)
    {
        var style = table.GridStyle;
        const int thickness = 1;
        var rowHeights = table.GetRowHeights();

        // 垂直网格线（列分隔）
        for (var colIndex = 1; colIndex < table.Cols; colIndex++)
        {
            var currentX = table.X + table.ColumnWidths.Take(colIndex).Sum();
            var segY = table.Y;
            for (var row = 0; row < table.Rows; row++)
            {
                var rowH = rowHeights[row];
                var skip = false;

                // 向左查找是否有 ColSpan 跨越此列边界
                for (var scanCol = colIndex - 1; scanCol >= 0; scanCol--)
                {
                    var scanIdx = row * table.Cols + scanCol;
                    if (scanIdx >= 0 && scanIdx < table.Cells.Count)
                    {
                        var scanCell = table.Cells[scanIdx];
                        if (!scanCell.IsMerged && scanCell.ColSpan > 1)
                        {
                            if (scanCol + scanCell.ColSpan > colIndex) skip = true;
                            break;
                        }
                        if (!scanCell.IsMerged) break;
                    }
                }

                if (!skip)
                {
                    foreach (var seg in DashSegmentCalculator.CalculateVerticalSegments(
                        currentX, segY, rowH, thickness, style))
                    {
                        builder.AppendLine($"BAR {seg.X},{seg.Y},{seg.Width},{seg.Height}");
                    }
                }
                segY += rowH;
            }
        }

        // 水平网格线（行分隔）
        for (var rowIndex = 0; rowIndex < table.Rows - 1; rowIndex++)
        {
            var currentY = table.Y + rowHeights.Take(rowIndex + 1).Sum();
            var segX = table.X;
            for (var col = 0; col < table.Cols; col++)
            {
                var colW = table.GetColumnWidth(col);
                var skip = false;

                // 向上查找是否有 RowSpan 跨越此行边界
                for (var scanRow = rowIndex; scanRow >= 0; scanRow--)
                {
                    var scanIdx = scanRow * table.Cols + col;
                    if (scanIdx >= 0 && scanIdx < table.Cells.Count)
                    {
                        var scanCell = table.Cells[scanIdx];
                        if (!scanCell.IsMerged && scanCell.RowSpan > 1)
                        {
                            if (scanRow + scanCell.RowSpan > rowIndex + 1) skip = true;
                            break;
                        }
                        if (!scanCell.IsMerged) break;
                    }
                }

                if (!skip)
                {
                    foreach (var seg in DashSegmentCalculator.CalculateHorizontalSegments(
                        segX, currentY, colW, thickness, style))
                    {
                        builder.AppendLine($"BAR {seg.X},{seg.Y},{seg.Width},{seg.Height}");
                    }
                }
                segX += colW;
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

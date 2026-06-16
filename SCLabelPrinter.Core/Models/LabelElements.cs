using System.Text.Json.Serialization;

namespace SCLabelPrinter.Core.Models;

/// <summary>
/// 定义标签元素的公共位置和旋转属性，支持未来扩展更多元素类型。
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(TextElement), "text")]
[JsonDerivedType(typeof(BarcodeElement), "barcode")]
[JsonDerivedType(typeof(QrCodeElement), "qrcode")]
[JsonDerivedType(typeof(BitmapElement), "bitmap")]
[JsonDerivedType(typeof(BoxElement), "box")]
[JsonDerivedType(typeof(LineElement), "line")]
[JsonDerivedType(typeof(EraseElement), "erase")]
[JsonDerivedType(typeof(TableElement), "table")]
public abstract class LabelElement
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public int X { get; set; }

    public int Y { get; set; }

    public int Rotation { get; set; }
}

/// <summary>
/// 表示文本元素。
/// </summary>
public sealed class TextElement : LabelElement
{
    public string Font { get; set; } = "3";

    /// <summary>
    /// 字体大小（单位：dot），0 表示使用 Font 字号的默认大小。
    /// </summary>
    public int FontSizeDots { get; set; }

    public int XScale { get; set; } = 1;

    public int YScale { get; set; } = 1;

    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 占位符名称，导出 TSPL 模板时替代实际内容，供外部软件按名称替换。
    /// 为空时直接输出 Content 内容。
    /// </summary>
    public string Placeholder { get; set; } = string.Empty;

    /// <summary>
    /// 返回适合界面列表展示的文本元素摘要。
    /// </summary>
    public override string ToString()
    {
        return $"文本: {Content}";
    }
}

/// <summary>
/// 表示一维条码元素。
/// </summary>
public sealed class BarcodeElement : LabelElement
{
    public BarcodeType CodeType { get; set; } = BarcodeType.Code128;

    public int Height { get; set; } = 80;

    public bool Readable { get; set; } = true;

    public int Narrow { get; set; } = 2;

    public int Wide { get; set; } = 2;

    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 占位符名称，导出模板时替代实际内容。
    /// </summary>
    public string Placeholder { get; set; } = string.Empty;

    /// <summary>
    /// 返回适合界面列表展示的条码元素摘要。
    /// </summary>
    public override string ToString()
    {
        return $"条码: {Content}";
    }
}

/// <summary>
/// 表示二维码元素。
/// </summary>
public sealed class QrCodeElement : LabelElement
{
    public string ErrorCorrectionLevel { get; set; } = "L";

    public int CellWidth { get; set; } = 5;

    public string Mode { get; set; } = "A";

    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 占位符名称，导出模板时替代实际内容。
    /// </summary>
    public string Placeholder { get; set; } = string.Empty;

    /// <summary>
    /// 返回适合界面列表展示的二维码元素摘要。
    /// </summary>
    public override string ToString()
    {
        return $"二维码: {Content}";
    }
}

/// <summary>
/// 表示位图元素。
/// </summary>
public sealed class BitmapElement : LabelElement, IResizable
{
    public int Width { get; set; }

    public int Height { get; set; }

    public int Mode { get; set; }

    public byte[] Data { get; set; } = Array.Empty<byte>();

    public int ElementWidth { get => Width; set => Width = value; }

    public int ElementHeight { get => Height; set => Height = value; }

    public int MinWidth => 8;

    public int MinHeight => 8;

    /// <summary>
    /// 返回适合界面列表展示的位图元素摘要。
    /// </summary>
    public override string ToString()
    {
        return $"位图: {Width}×{Height}";
    }
}

/// <summary>
/// 表示矩形框元素。
/// </summary>
public sealed class BoxElement : LabelElement, IResizable
{
    public int EndX { get; set; }

    public int EndY { get; set; }

    public int Thickness { get; set; } = 2;

    public int ElementWidth { get => EndX - X; set => EndX = X + value; }

    public int ElementHeight { get => EndY - Y; set => EndY = Y + value; }

    public int MinWidth => 4;

    public int MinHeight => 4;

    /// <summary>
    /// 返回适合界面列表展示的矩形框元素摘要。
    /// </summary>
    public override string ToString()
    {
        return $"矩形框: ({X},{Y})-({EndX},{EndY})";
    }
}

/// <summary>
/// 表示线条元素。
/// </summary>
public sealed class LineElement : LabelElement, IResizable
{
    public int Width { get; set; }

    public int Height { get; set; } = 2;

    public int ElementWidth { get => Width; set => Width = value; }

    public int ElementHeight { get => Height; set => Height = value; }

    public int MinWidth => 1;

    public int MinHeight => 1;

    /// <summary>
    /// 线条样式：Solid（实线）、Dashed（虚线）、Dotted（点线）。
    /// </summary>
    public TableLineStyle Style { get; set; } = TableLineStyle.Solid;

    /// <summary>
    /// 虚线模式下每个线段的长度（单位：dot）。
    /// </summary>
    public int DashLength { get; set; } = 8;

    /// <summary>
    /// 虚线模式下线段之间间隔的长度（单位：dot）。
    /// </summary>
    public int GapLength { get; set; } = 4;

    /// <summary>
    /// 返回适合界面列表展示的线条元素摘要。
    /// </summary>
    public override string ToString()
    {
        return $"线条: {Width}x{Height}";
    }
}

/// <summary>
/// 表示挖空区域元素，对应 TSPL ERASE 指令。
/// </summary>
public sealed class EraseElement : LabelElement, IResizable
{
    public int Width { get; set; }

    public int Height { get; set; }

    public int ElementWidth { get => Width; set => Width = value; }

    public int ElementHeight { get => Height; set => Height = value; }

    public int MinWidth => 1;

    public int MinHeight => 1;

    /// <summary>
    /// 返回适合界面列表展示的挖空元素摘要。
    /// </summary>
    public override string ToString()
    {
        return $"挖空: {Width}x{Height}";
    }
}

/// <summary>
/// 文本对齐方式。
/// </summary>
public enum LabelTextAlignment
{
    /// <summary>左对齐</summary>
    Left,
    /// <summary>居中</summary>
    Center,
    /// <summary>右对齐</summary>
    Right,
}

/// <summary>
/// 单元格内容类型，用于表格元素内部渲染与打印。
/// </summary>
public enum TableCellContentType
{
    Text,
    Barcode,
    QrCode,
}

/// <summary>
/// 表示表格线风格。
/// </summary>
public enum TableLineStyle
{
    /// <summary>实线</summary>
    Solid,
    /// <summary>虚线（长段间隔）</summary>
    Dashed,
    /// <summary>点线（短点间隔）</summary>
    Dotted,
}

/// <summary>
/// 表示单元格内部可拖拽的元素。
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(TableCellTextElement), "tableCellText")]
[JsonDerivedType(typeof(TableCellBarcodeElement), "tableCellBarcode")]
[JsonDerivedType(typeof(TableCellQrCodeElement), "tableCellQrCode")]
public abstract class TableCellInnerElement
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public int X { get; set; }

    public int Y { get; set; }

    public int Width { get; set; } = 120;

    public int Height { get; set; } = 40;

    public int Rotation { get; set; }
}

/// <summary>
/// 表示单元格内部文本元素。
/// </summary>
public sealed class TableCellTextElement : TableCellInnerElement
{
    public string Content { get; set; } = string.Empty;

    public string Font { get; set; } = "3";

    /// <summary>
    /// 文本对齐方式。
    /// </summary>
    public LabelTextAlignment Alignment { get; set; } = LabelTextAlignment.Left;

    public int XScale { get; set; } = 1;

    public int YScale { get; set; } = 1;

    public override string ToString()
    {
        return $"单元格文本: {Content}";
    }
}

/// <summary>
/// 表示单元格内部条码元素。
/// </summary>
public sealed class TableCellBarcodeElement : TableCellInnerElement
{
    public BarcodeType BarcodeType { get; set; } = BarcodeType.Code128;

    public int Narrow { get; set; } = 2;

    public int Wide { get; set; } = 2;

    public bool Readable { get; set; } = true;

    public string Content { get; set; } = string.Empty;

    public override string ToString()
    {
        return $"单元格条码: {Content}";
    }
}

/// <summary>
/// 表示单元格内部二维码元素。
/// </summary>
public sealed class TableCellQrCodeElement : TableCellInnerElement
{
    public string ErrorCorrectionLevel { get; set; } = "L";

    public int CellWidth { get; set; } = 5;

    public string Mode { get; set; } = "A";

    public string Content { get; set; } = string.Empty;

    public override string ToString()
    {
        return $"单元格二维码: {Content}";
    }
}

/// <summary>
/// 表示表格元素内的一个单元格。
/// </summary>
public sealed class TableCell
{
    public TableCellContentType ContentType { get; set; } = TableCellContentType.Text;

    /// <summary>
    /// 单元格文本对齐方式。
    /// </summary>
    public LabelTextAlignment Alignment { get; set; } = LabelTextAlignment.Left;

    public string Content { get; set; } = string.Empty;

    public BarcodeType BarcodeType { get; set; } = BarcodeType.Code128;

    public int QrCellWidth { get; set; } = 5;

    public string QrMode { get; set; } = "A";

    public string QrErrorCorrectionLevel { get; set; } = "L";

    public List<TableCellInnerElement> InnerElements { get; set; } = new();

    public bool HasLegacyContent => InnerElements.Count == 0;

    public void MigrateLegacyContentToInnerElements()
    {
        if (!HasLegacyContent || string.IsNullOrWhiteSpace(Content))
        {
            return;
        }

        switch (ContentType)
        {
            case TableCellContentType.Text:
                InnerElements.Add(new TableCellTextElement
                {
                    Content = Content,
                    Font = "3",
                });
                break;
            case TableCellContentType.Barcode:
                InnerElements.Add(new TableCellBarcodeElement
                {
                    Content = Content,
                    BarcodeType = BarcodeType,
                    Readable = true,
                    Narrow = 2,
                    Wide = 2,
                });
                break;
            case TableCellContentType.QrCode:
                InnerElements.Add(new TableCellQrCodeElement
                {
                    Content = Content,
                    ErrorCorrectionLevel = QrErrorCorrectionLevel,
                    CellWidth = QrCellWidth,
                    Mode = QrMode,
                });
                break;
        }
    }
}

/// <summary>
/// 表示表格元素，支持自定义行列和每个单元格的内容类型。
/// </summary>
public sealed class TableElement : LabelElement, IResizable
{
    public int ElementWidth { get => TotalWidth; set { /* 表格宽度由列宽决定 */ } }

    public int ElementHeight { get => TotalHeight; set { /* 表格高度由行高决定 */ } }

    public int MinWidth => Cols * 20;

    public int MinHeight => Rows * 20;

    public int Rows { get; set; } = 2;

    public int Cols { get; set; } = 2;

    public int RowHeight { get; set; } = 100;

    public List<int> RowHeights { get; set; } = new() { 100, 100 };

    public List<int> ColumnWidths { get; set; } = new() { 260, 260 };

    public List<TableCell> Cells { get; set; } = CreateDefaultCells(2, 2);

    public TableLineStyle BorderStyle { get; set; } = TableLineStyle.Dashed;

    public TableLineStyle GridStyle { get; set; } = TableLineStyle.Dashed;

    public int TotalWidth => ColumnWidths.Sum();

    public int TotalHeight => GetRowHeights().Sum();

    public int GetRowHeight(int index)
    {
        if (index < 0)
        {
            return 0;
        }

        var rowHeights = GetRowHeights();
        return index < rowHeights.Count ? rowHeights[index] : rowHeights.LastOrDefault();
    }

    public IReadOnlyList<int> GetRowHeights()
    {
        if (RowHeights is null || RowHeights.Count != Rows)
        {
            EnsureRowHeights();
        }

        return RowHeights;
    }

    public int GetColumnWidth(int index)
    {
        if (index < 0)
        {
            return 0;
        }

        return index < ColumnWidths.Count ? ColumnWidths[index] : ColumnWidths.LastOrDefault();
    }

    public void EnsureCellCount()
    {
        var required = Math.Max(0, Rows * Cols);
        while (Cells.Count < required)
        {
            Cells.Add(new TableCell());
        }

        if (Cells.Count > required)
        {
            Cells.RemoveRange(required, Cells.Count - required);
        }

        EnsureRowHeights();
    }

    public void InsertRowAt(int rowIndex)
    {
        if (rowIndex < 0 || rowIndex > Rows)
        {
            return;
        }

        var insertIndex = rowIndex * Cols;
        var newCells = CreateDefaultCells(1, Cols);
        Cells.InsertRange(insertIndex, newCells);
        Rows++;
        var defaultHeight = RowHeights.Count > 0 ? RowHeights[Math.Min(rowIndex, RowHeights.Count - 1)] : RowHeight;
        RowHeights.Insert(rowIndex, defaultHeight);
    }

    public void InsertColumnAt(int columnIndex)
    {
        if (columnIndex < 0 || columnIndex > Cols)
        {
            return;
        }

        var columnWidth = GetColumnWidth(Math.Max(0, columnIndex - 1));
        ColumnWidths.Insert(columnIndex, columnWidth > 0 ? columnWidth : 260);

        for (var row = Rows - 1; row >= 0; row--)
        {
            var insertIndex = row * (Cols + 1) + columnIndex;
            Cells.Insert(insertIndex, new TableCell());
        }

        Cols++;
    }

    public void RemoveRowAt(int rowIndex)
    {
        if (rowIndex < 0 || rowIndex >= Rows || Rows <= 1)
        {
            return;
        }

        var removeIndex = rowIndex * Cols;
        Cells.RemoveRange(removeIndex, Cols);
        Rows--;
        if (rowIndex < RowHeights.Count)
        {
            RowHeights.RemoveAt(rowIndex);
        }
    }

    public void RemoveColumnAt(int columnIndex)
    {
        if (columnIndex < 0 || columnIndex >= Cols || Cols <= 1)
        {
            return;
        }

        ColumnWidths.RemoveAt(columnIndex);

        for (var row = Rows - 1; row >= 0; row--)
        {
            var removeIndex = row * Cols + columnIndex;
            Cells.RemoveAt(removeIndex);
        }

        Cols--;
    }

    private void EnsureRowHeights()
    {
        if (RowHeights is null)
        {
            RowHeights = new List<int>(Rows);
        }

        while (RowHeights.Count < Rows)
        {
            RowHeights.Add(RowHeight);
        }

        if (RowHeights.Count > Rows)
        {
            RowHeights.RemoveRange(Rows, RowHeights.Count - Rows);
        }
    }

    private static List<TableCell> CreateDefaultCells(int rows, int cols)
    {
        var cells = new List<TableCell>();
        for (var row = 0; row < rows; row++)
        {
            for (var col = 0; col < cols; col++)
            {
                cells.Add(new TableCell());
            }
        }

        return cells;
    }

    /// <summary>
    /// 返回适合界面列表展示的表格元素摘要。
    /// </summary>
    public override string ToString()
    {
        return $"表格: {Rows}x{Cols}";
    }
}

/// <summary>
/// 表示表格右键菜单命令的请求参数。
/// </summary>
public sealed class TableCellContextMenuRequest
{
    public string TableElementId { get; set; } = string.Empty;

    public int Row { get; set; }

    public int Column { get; set; }

    public TableCellContextMenuAction Action { get; set; }
}

/// <summary>
/// 表示表格单元格右键菜单可执行的操作。
/// </summary>
public enum TableCellContextMenuAction
{
    AddRowAbove,
    AddRowBelow,
    RemoveRow,
    AddColumnLeft,
    AddColumnRight,
    RemoveColumn,
    AddCellTextElement,
    AddCellBarcodeElement,
    AddCellQrCodeElement,
    EditCell,
    EditCellInnerElement,
    RemoveCellInnerElement,
}

/// <summary>
/// 视图层通过画布拖动时传递的元素移动请求。
/// </summary>
public sealed record ElementMoveRequest(string ElementId, int X, int Y);

/// <summary>
/// 表示单元格内部元素移动请求。
/// </summary>
public sealed record TableCellInnerElementMoveRequest(string TableElementId, int Row, int Column, string InnerElementId, int X, int Y, int Width, int Height);

/// <summary>
/// 表示表格行或列大小调整请求。
/// </summary>
public enum TableCellResizeMode
{
    Column,
    Row,
}

/// <summary>
/// 表示表格行高或列宽调整请求。
/// </summary>
public sealed record TableCellResizeRequest(string TableElementId, TableCellResizeMode Mode, int Index, int NewSize, IReadOnlyList<int>? RowHeights = null, IReadOnlyList<int>? ColumnWidths = null);

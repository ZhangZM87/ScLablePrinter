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

    public int XScale { get; set; } = 1;

    public int YScale { get; set; } = 1;

    public string Content { get; set; } = string.Empty;

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
public sealed class BitmapElement : LabelElement
{
    public int Width { get; set; }

    public int Height { get; set; }

    public int Mode { get; set; }

    public byte[] Data { get; set; } = Array.Empty<byte>();

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
public sealed class BoxElement : LabelElement
{
    public int EndX { get; set; }

    public int EndY { get; set; }

    public int Thickness { get; set; } = 2;

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
public sealed class LineElement : LabelElement
{
    public int Width { get; set; }

    public int Height { get; set; } = 2;

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
public sealed class EraseElement : LabelElement
{
    public int Width { get; set; }

    public int Height { get; set; }

    /// <summary>
    /// 返回适合界面列表展示的挖空元素摘要。
    /// </summary>
    public override string ToString()
    {
        return $"挖空: {Width}x{Height}";
    }
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
/// 表示表格元素内的一个单元格。
/// </summary>
public sealed class TableCell
{
    public TableCellContentType ContentType { get; set; } = TableCellContentType.Text;

    public string Content { get; set; } = string.Empty;

    public BarcodeType BarcodeType { get; set; } = BarcodeType.Code128;

    public int QrCellWidth { get; set; } = 5;

    public string QrMode { get; set; } = "A";

    public string QrErrorCorrectionLevel { get; set; } = "L";
}

/// <summary>
/// 表示表格元素，支持自定义行列和每个单元格的内容类型。
/// </summary>
public sealed class TableElement : LabelElement
{
    public int Rows { get; set; } = 2;

    public int Cols { get; set; } = 2;

    public int RowHeight { get; set; } = 100;

    public List<int> ColumnWidths { get; set; } = new() { 260, 260 };

    public List<TableCell> Cells { get; set; } = CreateDefaultCells(2, 2);

    public int TotalWidth => ColumnWidths.Sum();

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
    EditCell,
}

/// <summary>
/// 视图层通过画布拖动时传递的元素移动请求。
/// </summary>
public sealed record ElementMoveRequest(string ElementId, int X, int Y);

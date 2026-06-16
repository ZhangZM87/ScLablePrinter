namespace SCLabelPrinter.Core.Models;

/// <summary>
/// 表示单元格或单元格内部元素在表格坐标系中的矩形边界。
/// </summary>
public readonly record struct TableCellBounds(int X, int Y, int Width, int Height)
{
    public int Right => X + Width;

    public int Bottom => Y + Height;
}

/// <summary>
/// 提供表格单元格与内部元素的统一几何计算，避免命中、拖动、缩放和绘制使用不同坐标规则。
/// </summary>
public static class TableCellLayoutCalculator
{
    /// <summary>
    /// 计算指定单元格在表格坐标系中的边界。
    /// </summary>
    public static TableCellBounds GetCellBounds(TableElement table, int rowIndex, int columnIndex)
    {
        ArgumentNullException.ThrowIfNull(table);

        ValidateCellIndex(table, rowIndex, columnIndex);
        var x = 0;
        for (var index = 0; index < columnIndex; index++)
        {
            x += table.GetColumnWidth(index);
        }

        var y = 0;
        for (var index = 0; index < rowIndex; index++)
        {
            y += table.GetRowHeight(index);
        }

        return new TableCellBounds(x, y, table.GetColumnWidth(columnIndex), table.GetRowHeight(rowIndex));
    }

    /// <summary>
    /// 获取考虑合并后的单元格边界（跨多列/行时返回合并后的总宽高）。
    /// </summary>
    public static TableCellBounds GetMergedCellBounds(TableElement table, int rowIndex, int columnIndex)
    {
        ArgumentNullException.ThrowIfNull(table);
        var cellIndex = rowIndex * table.Cols + columnIndex;
        if (cellIndex < 0 || cellIndex >= table.Cells.Count)
        {
            return new TableCellBounds(0, 0, 0, 0);
        }

        var cell = table.Cells[cellIndex];
        var baseBounds = GetCellBounds(table, rowIndex, columnIndex);

        if (cell.ColSpan <= 1 && cell.RowSpan <= 1)
        {
            return baseBounds;
        }

        var totalWidth = 0;
        for (var col = columnIndex; col < columnIndex + cell.ColSpan && col < table.Cols; col++)
        {
            totalWidth += table.GetColumnWidth(col);
        }

        var totalHeight = 0;
        for (var row = rowIndex; row < rowIndex + cell.RowSpan && row < table.Rows; row++)
        {
            totalHeight += table.GetRowHeight(row);
        }

        return new TableCellBounds(baseBounds.X, baseBounds.Y, totalWidth, totalHeight);
    }

    /// <summary>
    /// 计算指定内部元素在表格坐标系中的边界。
    /// </summary>
    public static TableCellBounds GetInnerElementBounds(TableElement table, int rowIndex, int columnIndex, TableCellInnerElement innerElement)
    {
        ArgumentNullException.ThrowIfNull(innerElement);

        var cellBounds = GetCellBounds(table, rowIndex, columnIndex);
        return new TableCellBounds(
            cellBounds.X + innerElement.X,
            cellBounds.Y + innerElement.Y,
            Math.Max(1, innerElement.Width),
            Math.Max(1, innerElement.Height));
    }

    /// <summary>
    /// 将内部元素的拖动坐标限制在单元格范围内。
    /// </summary>
    public static (int X, int Y) ClampInnerElementPosition(TableElement table, int rowIndex, int columnIndex, int innerWidth, int innerHeight, int targetX, int targetY)
    {
        var cellBounds = GetCellBounds(table, rowIndex, columnIndex);
        var maxX = Math.Max(0, cellBounds.Width - Math.Max(1, innerWidth));
        var maxY = Math.Max(0, cellBounds.Height - Math.Max(1, innerHeight));
        return (Math.Clamp(targetX, 0, maxX), Math.Clamp(targetY, 0, maxY));
    }

    /// <summary>
    /// 将内部元素的缩放结果限制在单元格剩余可用区域内。
    /// </summary>
    public static (int Width, int Height) ClampInnerElementSize(TableElement table, int rowIndex, int columnIndex, int innerX, int innerY, int targetWidth, int targetHeight, int minimumSize = 20)
    {
        var cellBounds = GetCellBounds(table, rowIndex, columnIndex);
        var availableWidth = Math.Max(1, cellBounds.Width - Math.Max(0, innerX));
        var availableHeight = Math.Max(1, cellBounds.Height - Math.Max(0, innerY));
        var width = Math.Min(Math.Max(minimumSize, targetWidth), availableWidth);
        var height = Math.Min(Math.Max(minimumSize, targetHeight), availableHeight);
        return (width, height);
    }

    /// <summary>
    /// 将内部元素放入单元格可用内容区域，统一默认位置与尺寸初始化规则。
    /// </summary>
    public static void FitInnerElementToCell(TableElement table, int rowIndex, int columnIndex, TableCellInnerElement innerElement, int padding)
    {
        ArgumentNullException.ThrowIfNull(innerElement);

        var cellBounds = GetCellBounds(table, rowIndex, columnIndex);
        var safePadding = Math.Max(0, padding);
        var availableWidth = Math.Max(1, cellBounds.Width - safePadding * 2);
        var availableHeight = Math.Max(1, cellBounds.Height - safePadding * 2);

        innerElement.X = safePadding;
        innerElement.Y = safePadding;
        innerElement.Width = Math.Min(Math.Max(20, innerElement.Width), availableWidth);
        innerElement.Height = Math.Min(Math.Max(20, innerElement.Height), availableHeight);
    }

    /// <summary>
    /// 校验单元格索引有效性，避免调用方在越界时继续产生错误几何结果。
    /// </summary>
    private static void ValidateCellIndex(TableElement table, int rowIndex, int columnIndex)
    {
        if (rowIndex < 0 || rowIndex >= table.Rows)
        {
            throw new ArgumentOutOfRangeException(nameof(rowIndex));
        }

        if (columnIndex < 0 || columnIndex >= table.Cols)
        {
            throw new ArgumentOutOfRangeException(nameof(columnIndex));
        }
    }
}
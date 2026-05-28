namespace SCLabelPrinter.Core.Models;

/// <summary>
/// 为表格缩放提交构造稳定的请求快照，确保预览态与最终持久化态保持一致。
/// </summary>
public static class TableCellResizeRequestFactory
{
    /// <summary>
    /// 根据当前表格状态创建列宽调整请求。
    /// </summary>
    public static TableCellResizeRequest CreateColumnRequest(TableElement table, int columnIndex)
    {
        ArgumentNullException.ThrowIfNull(table);

        var safeColumnIndex = NormalizeColumnIndex(table, columnIndex);
        return new TableCellResizeRequest(
            table.Id,
            TableCellResizeMode.Column,
            safeColumnIndex,
            table.GetColumnWidth(safeColumnIndex));
    }

    /// <summary>
    /// 根据当前表格状态创建行高调整请求，并附带完整行高快照以保留联动变更。
    /// </summary>
    public static TableCellResizeRequest CreateRowRequest(TableElement table, int rowIndex)
    {
        ArgumentNullException.ThrowIfNull(table);

        var safeRowIndex = NormalizeRowIndex(table, rowIndex);
        var rowHeights = table.GetRowHeights().ToArray();
        return new TableCellResizeRequest(
            table.Id,
            TableCellResizeMode.Row,
            safeRowIndex,
            rowHeights[safeRowIndex],
            rowHeights);
    }

    /// <summary>
    /// 根据当前表格状态创建整体拖动需要提交的列宽与行高请求集合。
    /// </summary>
    public static IReadOnlyList<TableCellResizeRequest> CreateOverallRequests(TableElement table, int columnIndex, int rowIndex)
    {
        ArgumentNullException.ThrowIfNull(table);

        return
        [
            CreateColumnRequest(table, columnIndex),
            CreateRowRequest(table, rowIndex),
        ];
    }

    /// <summary>
    /// 规范化列索引，确保外部调用即使传入边界值也能得到稳定结果。
    /// </summary>
    private static int NormalizeColumnIndex(TableElement table, int columnIndex)
    {
        if (table.Cols <= 0 || table.ColumnWidths.Count == 0)
        {
            throw new InvalidOperationException("表格未定义可调整的列宽。");
        }

        return Math.Clamp(columnIndex, 0, table.ColumnWidths.Count - 1);
    }

    /// <summary>
    /// 规范化行索引，并在需要时先补齐行高集合，保证请求快照完整可序列化。
    /// </summary>
    private static int NormalizeRowIndex(TableElement table, int rowIndex)
    {
        if (table.Rows <= 0)
        {
            throw new InvalidOperationException("表格未定义可调整的行高。");
        }

        table.EnsureCellCount();
        return Math.Clamp(rowIndex, 0, table.Rows - 1);
    }
}
namespace SCLabelPrinter.Core.Models;

/// <summary>
/// 表示表格内部元素在表格坐标系中的统一视觉布局结果。
/// </summary>
public readonly record struct TableCellInnerElementVisualLayout(
    TableCellBounds FrameBounds,
    TableCellBounds ContentBounds,
    TableInteractionPoint RotationAnchor);

/// <summary>
/// 定义表格内部元素的视觉布局计算接口，避免绘制、命中和选区各自维护一套几何规则。
/// </summary>
public interface ITableCellInnerElementVisualLayoutService
{
    /// <summary>
    /// 根据单元格和内部元素数据生成统一的框体、内容区和旋转锚点。
    /// </summary>
    TableCellInnerElementVisualLayout CreateLayout(TableElement table, int rowIndex, int columnIndex, TableCellInnerElement innerElement, int contentPadding);
}

/// <summary>
/// 提供表格内部元素的统一视觉布局计算，实现绘制框体、内容裁剪和旋转锚点的一致性。
/// </summary>
public sealed class TableCellInnerElementVisualLayoutService : ITableCellInnerElementVisualLayoutService
{
    /// <summary>
    /// 根据内部元素边界和内容内边距生成视觉布局，保证后续绘制最小仍有可见区域。
    /// </summary>
    public TableCellInnerElementVisualLayout CreateLayout(TableElement table, int rowIndex, int columnIndex, TableCellInnerElement innerElement, int contentPadding)
    {
        ArgumentNullException.ThrowIfNull(table);
        ArgumentNullException.ThrowIfNull(innerElement);

        var frameBounds = TableCellLayoutCalculator.GetInnerElementBounds(table, rowIndex, columnIndex, innerElement);
        var safePadding = Math.Max(0, contentPadding);
        var inset = safePadding * 2;
        var contentWidth = Math.Max(1, frameBounds.Width - inset);
        var contentHeight = Math.Max(1, frameBounds.Height - inset);
        var contentBounds = new TableCellBounds(
            frameBounds.X + safePadding,
            frameBounds.Y + safePadding,
            contentWidth,
            contentHeight);

        return new TableCellInnerElementVisualLayout(
            frameBounds,
            contentBounds,
            new TableInteractionPoint(frameBounds.X, frameBounds.Y));
    }
}
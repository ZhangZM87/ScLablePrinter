namespace SCLabelPrinter.Core.Models;

/// <summary>
/// 表示表格坐标系中的交互点，统一拖拽命中、偏移和定位计算的数据载体。
/// </summary>
public readonly record struct TableInteractionPoint(double X, double Y);

/// <summary>
/// 定义表格单元格内部元素的交互几何计算接口，避免 UI 层直接分散处理旋转和约束逻辑。
/// </summary>
public interface ITableCellInnerElementInteractionService
{
    /// <summary>
    /// 捕获当前鼠标点相对于内部元素锚点的局部偏移，确保后续拖拽保持同一附着点。
    /// </summary>
    TableInteractionPoint CaptureDragOffset(TableElement table, int rowIndex, int columnIndex, TableCellInnerElement innerElement, TableInteractionPoint pointerPosition);

    /// <summary>
    /// 根据当前鼠标点和起始偏移解析新的内部元素位置，并统一执行单元格边界约束。
    /// </summary>
    (int X, int Y) ResolveDragPosition(TableElement table, int rowIndex, int columnIndex, TableCellInnerElement innerElement, TableInteractionPoint pointerPosition, TableInteractionPoint dragOffset);
}

/// <summary>
/// 提供表格内部元素拖拽所需的旋转坐标换算与边界约束，实现预览命中与最终位置计算的一致性。
/// </summary>
public sealed class TableCellInnerElementInteractionService : ITableCellInnerElementInteractionService
{
    /// <summary>
    /// 捕获拖拽开始时的局部偏移，将鼠标点反变换到元素自身坐标系中。
    /// </summary>
    public TableInteractionPoint CaptureDragOffset(TableElement table, int rowIndex, int columnIndex, TableCellInnerElement innerElement, TableInteractionPoint pointerPosition)
    {
        ArgumentNullException.ThrowIfNull(table);
        ArgumentNullException.ThrowIfNull(innerElement);

        var localPointer = new TableInteractionPoint(
            pointerPosition.X - innerElement.X,
            pointerPosition.Y - innerElement.Y);
        return RotateVector(localPointer, -NormalizeRotation(innerElement.Rotation));
    }

    /// <summary>
    /// 根据当前鼠标点还原内部元素新的锚点位置，并将结果限制在当前单元格内部。
    /// </summary>
    public (int X, int Y) ResolveDragPosition(TableElement table, int rowIndex, int columnIndex, TableCellInnerElement innerElement, TableInteractionPoint pointerPosition, TableInteractionPoint dragOffset)
    {
        ArgumentNullException.ThrowIfNull(table);
        ArgumentNullException.ThrowIfNull(innerElement);

        var pointerDelta = RotateVector(dragOffset, NormalizeRotation(innerElement.Rotation));
        var absoluteX = pointerPosition.X - pointerDelta.X;
        var absoluteY = pointerPosition.Y - pointerDelta.Y;
        var targetX = (int)Math.Round(absoluteX);
        var targetY = (int)Math.Round(absoluteY);

        return TableCellLayoutCalculator.ClampInnerElementPosition(
            table,
            rowIndex,
            columnIndex,
            innerElement.Width,
            innerElement.Height,
            targetX,
            targetY);
    }

    /// <summary>
    /// 旋转局部偏移向量，统一正向和反向坐标变换的实现。
    /// </summary>
    private static TableInteractionPoint RotateVector(TableInteractionPoint vector, int rotation)
    {
        if (rotation % 360 == 0)
        {
            return vector;
        }

        var radians = rotation * Math.PI / 180d;
        var cosine = Math.Cos(radians);
        var sine = Math.Sin(radians);
        return new TableInteractionPoint(
            vector.X * cosine - vector.Y * sine,
            vector.X * sine + vector.Y * cosine);
    }

    /// <summary>
    /// 规范化旋转角度，避免负角度或大角度输入影响交互结果稳定性。
    /// </summary>
    private static int NormalizeRotation(int rotation)
    {
        return ((rotation % 360) + 360) % 360;
    }
}
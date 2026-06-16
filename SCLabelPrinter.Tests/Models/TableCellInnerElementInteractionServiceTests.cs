using SCLabelPrinter.Core.Models;

namespace SCLabelPrinter.Tests.Models;

[TestClass]
public sealed class TableCellInnerElementInteractionServiceTests
{
    /// <summary>
    /// 验证旋转内部元素在拖拽时会保持相同的鼠标附着点，避免命中坐标与绘制坐标分离后出现回跳。
    /// </summary>
    [TestMethod]
    public void ResolveDragPosition_ShouldKeepPointerAttachedForRotatedElement()
    {
        ITableCellInnerElementInteractionService service = new TableCellInnerElementInteractionService();
        var table = new TableElement
        {
            Rows = 1,
            Cols = 1,
            RowHeights = [200],
            ColumnWidths = [200],
        };
        var innerElement = new TableCellTextElement
        {
            X = 30,
            Y = 40,
            Width = 60,
            Height = 30,
            Rotation = 90,
        };

        var dragOffset = service.CaptureDragOffset(
            table,
            0,
            0,
            innerElement,
            new TableInteractionPoint(22, 45));

        Assert.AreEqual(5d, dragOffset.X, 0.001d);
        Assert.AreEqual(8d, dragOffset.Y, 0.001d);

        var position = service.ResolveDragPosition(
            table,
            0,
            0,
            innerElement,
            new TableInteractionPoint(62, 75),
            dragOffset);

        Assert.AreEqual(70, position.X);
        Assert.AreEqual(70, position.Y);
    }

    /// <summary>
    /// 验证拖拽结果仍会被限制在单元格范围内，避免引入新的越界行为。
    /// </summary>
    [TestMethod]
    public void ResolveDragPosition_ShouldClampResolvedPositionWithinCell()
    {
        ITableCellInnerElementInteractionService service = new TableCellInnerElementInteractionService();
        var table = new TableElement
        {
            Rows = 1,
            Cols = 1,
            RowHeights = [80],
            ColumnWidths = [100],
        };
        var innerElement = new TableCellTextElement
        {
            X = 10,
            Y = 10,
            Width = 60,
            Height = 30,
        };

        var dragOffset = service.CaptureDragOffset(
            table,
            0,
            0,
            innerElement,
            new TableInteractionPoint(25, 20));

        var position = service.ResolveDragPosition(
            table,
            0,
            0,
            innerElement,
            new TableInteractionPoint(180, 150),
            dragOffset);

        Assert.AreEqual(40, position.X);
        Assert.AreEqual(50, position.Y);
    }
}
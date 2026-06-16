using SCLabelPrinter.Core.Models;

namespace SCLabelPrinter.Tests.Models;

[TestClass]
public sealed class TableCellInnerElementVisualLayoutServiceTests
{
    /// <summary>
    /// 验证文本内部元素的视觉布局会保留完整框体，并基于统一内边距计算内容区域。
    /// </summary>
    [TestMethod]
    public void CreateLayout_ShouldPreserveFrameBoundsAndInsetContentBounds()
    {
        ITableCellInnerElementVisualLayoutService service = new TableCellInnerElementVisualLayoutService();
        var table = new TableElement
        {
            Rows = 2,
            Cols = 2,
            RowHeights = [40, 50],
            ColumnWidths = [60, 80],
        };
        var innerElement = new TableCellTextElement
        {
            X = 6,
            Y = 8,
            Width = 48,
            Height = 28,
        };

        var layout = service.CreateLayout(table, 1, 1, innerElement, 2);

        Assert.AreEqual(new TableCellBounds(66, 48, 48, 28), layout.FrameBounds);
        Assert.AreEqual(new TableCellBounds(68, 50, 44, 24), layout.ContentBounds);
        Assert.AreEqual(new TableInteractionPoint(66, 48), layout.RotationAnchor);
    }

    /// <summary>
    /// 验证过小的内部元素仍会生成最小可绘制的内容区域，避免后续绘制分支出现零尺寸。
    /// </summary>
    [TestMethod]
    public void CreateLayout_ShouldClampContentBoundsToMinimumVisibleSize()
    {
        ITableCellInnerElementVisualLayoutService service = new TableCellInnerElementVisualLayoutService();
        var table = new TableElement
        {
            Rows = 1,
            Cols = 1,
            RowHeights = [40],
            ColumnWidths = [40],
        };
        var innerElement = new TableCellTextElement
        {
            X = 1,
            Y = 1,
            Width = 3,
            Height = 3,
        };

        var layout = service.CreateLayout(table, 0, 0, innerElement, 2);

        Assert.AreEqual(new TableCellBounds(1, 1, 3, 3), layout.FrameBounds);
        Assert.AreEqual(new TableCellBounds(3, 3, 1, 1), layout.ContentBounds);
        Assert.AreEqual(new TableInteractionPoint(1, 1), layout.RotationAnchor);
    }
}
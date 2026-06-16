using SCLabelPrinter.Core.Models;

namespace SCLabelPrinter.Tests.Models;

[TestClass]
public sealed class TableCellLayoutCalculatorTests
{
    /// <summary>
    /// 验证单元格边界会正确累计前置行高与列宽，避免不同调用点各自计算坐标而出现错位。
    /// </summary>
    [TestMethod]
    public void GetCellBounds_ShouldAccumulatePreviousRowsAndColumns()
    {
        var table = new TableElement
        {
            Rows = 3,
            Cols = 3,
            RowHeights = [40, 60, 80],
            ColumnWidths = [50, 70, 90],
        };

        var bounds = TableCellLayoutCalculator.GetCellBounds(table, 2, 1);

        Assert.AreEqual(50, bounds.X);
        Assert.AreEqual(100, bounds.Y);
        Assert.AreEqual(70, bounds.Width);
        Assert.AreEqual(80, bounds.Height);
    }

    /// <summary>
    /// 验证内部元素拖动会被限制在当前单元格范围内，避免拖出单元格后命中与渲染继续错乱。
    /// </summary>
    [TestMethod]
    public void ClampInnerElementPosition_ShouldKeepElementInsideCell()
    {
        var table = new TableElement
        {
            Rows = 1,
            Cols = 1,
            RowHeights = [80],
            ColumnWidths = [100],
        };

        var position = TableCellLayoutCalculator.ClampInnerElementPosition(table, 0, 0, 70, 30, 60, 70);

        Assert.AreEqual(30, position.X);
        Assert.AreEqual(50, position.Y);
    }
}
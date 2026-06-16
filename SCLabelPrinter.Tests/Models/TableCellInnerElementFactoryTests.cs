using SCLabelPrinter.Core.Models;

namespace SCLabelPrinter.Tests.Models;

[TestClass]
public sealed class TableCellInnerElementFactoryTests
{
    /// <summary>
    /// 验证新增单元格文本元素会按单元格可用区域初始化，避免默认外框明显超出内容区域。
    /// </summary>
    [TestMethod]
    public void CreateTextElement_ShouldFitWithinCellContentArea()
    {
        ITableCellTextPreviewMetricsService metricsService = new TableCellTextPreviewMetricsService();
        var table = new TableElement
        {
            Rows = 1,
            Cols = 1,
            RowHeights = [48],
            ColumnWidths = [72],
        };

        var element = TableCellInnerElementFactory.CreateTextElement(table, 0, 0, "文本");

        Assert.AreEqual(6, element.X);
        Assert.AreEqual(6, element.Y);
        Assert.AreEqual(48, element.Width);
        Assert.AreEqual(metricsService.GetMinimumFrameHeight(element.Font, element.YScale, 2), element.Height);
    }
}
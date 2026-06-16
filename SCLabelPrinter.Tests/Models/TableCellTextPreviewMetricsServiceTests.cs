using SCLabelPrinter.Core.Models;

namespace SCLabelPrinter.Tests.Models;

[TestClass]
public sealed class TableCellTextPreviewMetricsServiceTests
{
    /// <summary>
    /// 验证字体 3 的默认预览至少需要一个完整单行高度，避免文本元素创建后即处于不可见状态。
    /// </summary>
    [TestMethod]
    public void GetMinimumFrameHeight_ShouldReserveEnoughHeightForDefaultTextElement()
    {
        ITableCellTextPreviewMetricsService service = new TableCellTextPreviewMetricsService();

        var frameHeight = service.GetMinimumFrameHeight("3", 1, 2);

        Assert.IsTrue(frameHeight > 28);
    }
}
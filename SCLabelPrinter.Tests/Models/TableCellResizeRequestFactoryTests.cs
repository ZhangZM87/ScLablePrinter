using SCLabelPrinter.Core.Models;

namespace SCLabelPrinter.Tests.Models;

[TestClass]
public sealed class TableCellResizeRequestFactoryTests
{
    /// <summary>
    /// 验证行高请求会携带完整的行高快照，避免联动调整时只持久化单行状态。
    /// </summary>
    [TestMethod]
    public void CreateRowRequest_ShouldCaptureCompleteRowHeightsSnapshot()
    {
        var table = new TableElement
        {
            Id = "table-1",
            Rows = 3,
            Cols = 2,
            RowHeights = [90, 110, 130],
            ColumnWidths = [180, 220],
        };

        var request = TableCellResizeRequestFactory.CreateRowRequest(table, 1);

        Assert.AreEqual(TableCellResizeMode.Row, request.Mode);
        Assert.AreEqual(1, request.Index);
        Assert.AreEqual(110, request.NewSize);
        CollectionAssert.AreEqual(new[] { 90, 110, 130 }, request.RowHeights?.ToArray());
    }

    /// <summary>
    /// 验证整体拖动会拆分为列请求和带完整行高快照的行请求。
    /// </summary>
    [TestMethod]
    public void CreateOverallRequests_ShouldReturnColumnAndRowSnapshotRequests()
    {
        var table = new TableElement
        {
            Id = "table-2",
            Rows = 2,
            Cols = 2,
            RowHeights = [100, 140],
            ColumnWidths = [160, 240],
        };

        var requests = TableCellResizeRequestFactory.CreateOverallRequests(table, 1, 1);

        Assert.AreEqual(2, requests.Count);
        Assert.AreEqual(TableCellResizeMode.Column, requests[0].Mode);
        Assert.AreEqual(240, requests[0].NewSize);
        Assert.AreEqual(TableCellResizeMode.Row, requests[1].Mode);
        CollectionAssert.AreEqual(new[] { 100, 140 }, requests[1].RowHeights?.ToArray());
    }
}
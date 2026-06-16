using SCLabelPrinter.Core.Models;

namespace SCLabelPrinter.Tests.Models;

[TestClass]
public sealed class TableCellResizeRequestFactoryTests
{
    /// <summary>
    /// 验证列宽请求会携带完整的列宽快照，避免联动列缩放时只持久化单列状态。
    /// </summary>
    [TestMethod]
    public void CreateColumnRequest_ShouldCaptureCompleteColumnWidthsSnapshot()
    {
        var table = new TableElement
        {
            Id = "table-0",
            Rows = 2,
            Cols = 3,
            RowHeights = [90, 110],
            ColumnWidths = [120, 140, 160],
        };

        var request = TableCellResizeRequestFactory.CreateColumnRequest(table, 1);

        Assert.AreEqual(TableCellResizeMode.Column, request.Mode);
        Assert.AreEqual(1, request.Index);
        Assert.AreEqual(140, request.NewSize);
        CollectionAssert.AreEqual(new[] { 120, 140, 160 }, request.ColumnWidths?.ToArray());
    }

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
        CollectionAssert.AreEqual(new[] { 160, 240 }, requests[0].ColumnWidths?.ToArray());
        Assert.AreEqual(TableCellResizeMode.Row, requests[1].Mode);
        CollectionAssert.AreEqual(new[] { 100, 140 }, requests[1].RowHeights?.ToArray());
    }

    /// <summary>
    /// 验证交互式联动调整两行后创建的请求仍保留完整快照，避免预览联动在提交后丢失。
    /// </summary>
    [TestMethod]
    public void CreateRowRequest_ShouldPreserveAllRowHeightsAfterInteractiveAdjustment()
    {
        var table = new TableElement
        {
            Id = "table-3",
            Rows = 3,
            Cols = 1,
            RowHeights = [80, 100, 120],
            ColumnWidths = [160],
        };

        table.RowHeights[0] = 120;
        table.RowHeights[1] = 60;

        var request = TableCellResizeRequestFactory.CreateRowRequest(table, 0);

        Assert.AreEqual(TableCellResizeMode.Row, request.Mode);
        Assert.AreEqual(0, request.Index);
        Assert.AreEqual(120, request.NewSize);
        CollectionAssert.AreEqual(new[] { 120, 60, 120 }, request.RowHeights?.ToArray());
    }
}
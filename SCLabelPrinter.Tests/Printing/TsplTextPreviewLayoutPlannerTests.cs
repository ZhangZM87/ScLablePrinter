using SCLabelPrinter.Core.Models;
using SCLabelPrinter.Core.Printing;

namespace SCLabelPrinter.Tests.Printing;

/// <summary>
/// 验证 TSPL 文本预览布局规划在边界和截断上的约束行为。
/// </summary>
[TestClass]
public sealed class TsplTextPreviewLayoutPlannerTests
{
    /// <summary>
    /// 当文本元素接近标签右侧边缘时，预览宽度必须被限制在标签剩余空间内。
    /// </summary>
    [TestMethod]
    public void Plan_ShouldClampWidthToRemainingLabelSpace()
    {
        var planner = new TsplTextPreviewLayoutPlanner();
        var label = new LabelDefinition
        {
            Width = 60,
            Height = 40,
            Unit = LabelUnit.Millimeter,
        };
        var element = new TextElement
        {
            X = 420,
            Y = 16,
            Font = "4",
            XScale = 1,
            YScale = 1,
            Content = "ABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890",
        };

        var plan = planner.Plan(label, element);

        Assert.IsTrue(plan.MaxWidthDots > 0);
        Assert.IsTrue(plan.MaxWidthDots <= 60 * 8 - 420);
        Assert.AreEqual(4, plan.MaxLines);
    }

    /// <summary>
    /// 长文本的预览高度必须受最大展示行数控制，并标记为需要截断。
    /// </summary>
    [TestMethod]
    public void Plan_ShouldLimitHeightAndMarkTrimmedForLongContent()
    {
        var planner = new TsplTextPreviewLayoutPlanner();
        var label = new LabelDefinition
        {
            Width = 128,
            Height = 150,
            Unit = LabelUnit.Millimeter,
        };
        var element = new TextElement
        {
            X = 24,
            Y = 30,
            Font = "3",
            XScale = 1,
            YScale = 2,
            Content = new string('A', 220),
        };

        var plan = planner.Plan(label, element);

        Assert.IsTrue(plan.ShouldTrim);
        Assert.IsTrue(plan.EstimatedLineCount > plan.MaxLines);
        Assert.AreEqual(plan.LineHeightDots * plan.MaxLines, plan.MaxHeightDots, 0.001);
    }
}
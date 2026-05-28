using System.Xml.Linq;

namespace SCLabelPrinter.Tests.Views;

[TestClass]
public sealed class FilePrintViewMarkupTests
{
    /// <summary>
    /// 验证快速打印预览中的 LabelCanvas 放在可滚动容器内，并固定左上对齐，避免大尺寸预览挤出卡片区域。
    /// </summary>
    [TestMethod]
    public void FilePrintView_ShouldWrapLabelCanvasInsideScrollViewer()
    {
        var document = XDocument.Load(GetFilePrintViewPath());
        var labelCanvas = document.Descendants().FirstOrDefault(node => node.Name.LocalName == "LabelCanvas");

        Assert.IsNotNull(labelCanvas);
        Assert.IsTrue(labelCanvas.Ancestors().Any(node => node.Name.LocalName == "ScrollViewer"));
        Assert.AreEqual("Left", (string?)labelCanvas.Attribute("HorizontalAlignment"));
        Assert.AreEqual("Top", (string?)labelCanvas.Attribute("VerticalAlignment"));
    }

    /// <summary>
    /// 解析工作区中的 FilePrintView.xaml 绝对路径，供结构测试读取原始 XAML。
    /// </summary>
    private static string GetFilePrintViewPath()
    {
        return Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "SCLabelPrinter",
            "Views",
            "FilePrintView.xaml"));
    }
}
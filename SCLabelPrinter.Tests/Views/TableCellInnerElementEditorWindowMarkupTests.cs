using System.Xml.Linq;

namespace SCLabelPrinter.Tests.Views;

[TestClass]
public sealed class TableCellInnerElementEditorWindowMarkupTests
{
    /// <summary>
    /// 验证内部元素编辑窗口不再依赖失效的 DisplayMemberPath=ToString，并保留按类型分区的编辑面板。
    /// </summary>
    [TestMethod]
    public void TableCellInnerElementEditorWindow_ShouldUseItemTemplateAndTypedSections()
    {
        var document = XDocument.Load(GetMarkupPath());
        var listBox = document.Descendants().FirstOrDefault(node => node.Name.LocalName == "ListBox");
        var itemTemplate = listBox?.Descendants().FirstOrDefault(node => node.Name.LocalName == "DataTemplate");
        var textBlocks = document.Descendants().Where(node => node.Name.LocalName == "TextBlock").ToList();

        Assert.IsNotNull(listBox);
        Assert.IsFalse(listBox!.Attributes().Any(attribute => attribute.Name.LocalName == "DisplayMemberPath"));
        Assert.IsNotNull(itemTemplate);
        Assert.IsTrue(textBlocks.Any(node => (string?)node.Attribute("Text") == "文本设置"));
        Assert.IsTrue(textBlocks.Any(node => (string?)node.Attribute("Text") == "条码设置"));
        Assert.IsTrue(textBlocks.Any(node => (string?)node.Attribute("Text") == "二维码设置"));
    }

    /// <summary>
    /// 解析工作区中的 TableCellInnerElementEditorWindow.xaml 绝对路径，供结构测试读取原始 XAML。
    /// </summary>
    private static string GetMarkupPath()
    {
        return Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "SCLabelPrinter",
            "Views",
            "TableCellInnerElementEditorWindow.xaml"));
    }
}
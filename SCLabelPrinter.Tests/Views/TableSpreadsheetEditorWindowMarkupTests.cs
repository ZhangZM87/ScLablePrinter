using System.Xml.Linq;

namespace SCLabelPrinter.Tests.Views;

[TestClass]
public sealed class TableSpreadsheetEditorWindowMarkupTests
{
    /// <summary>
    /// 验证表格编辑窗口包含 DataGrid 主体以及常用的行列操作按钮，保证电子表格式编辑基础结构存在。
    /// </summary>
    [TestMethod]
    public void TableSpreadsheetEditorWindow_ShouldExposeSpreadsheetGridAndStructureActions()
    {
        var document = XDocument.Load(GetMarkupPath());
        var dataGrid = document.Descendants().FirstOrDefault(node => node.Name.LocalName == "DataGrid");
        var buttons = document.Descendants().Where(node => node.Name.LocalName == "Button").ToList();

        Assert.IsNotNull(dataGrid);
        Assert.IsTrue(buttons.Any(node => (string?)node.Attribute("Content") == "上方插行"));
        Assert.IsTrue(buttons.Any(node => (string?)node.Attribute("Content") == "下方插行"));
        Assert.IsTrue(buttons.Any(node => (string?)node.Attribute("Content") == "删除当前行"));
        Assert.IsTrue(buttons.Any(node => (string?)node.Attribute("Content") == "左侧插列"));
        Assert.IsTrue(buttons.Any(node => (string?)node.Attribute("Content") == "右侧插列"));
        Assert.IsTrue(buttons.Any(node => (string?)node.Attribute("Content") == "删除当前列"));
    }

    /// <summary>
    /// 解析工作区中的 TableSpreadsheetEditorWindow.xaml 绝对路径，供结构测试读取原始 XAML。
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
            "TableSpreadsheetEditorWindow.xaml"));
    }
}
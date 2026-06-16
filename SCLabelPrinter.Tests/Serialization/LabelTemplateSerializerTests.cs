using SCLabelPrinter.Core.Models;
using SCLabelPrinter.Core.Serialization;

namespace SCLabelPrinter.Tests.Serialization;

[TestClass]
public sealed class LabelTemplateSerializerTests
{
    [TestMethod]
    public void SerializeAndDeserialize_ShouldPreserveDerivedElementTypes()
    {
        var serializer = new LabelTemplateSerializer();
        var template = new LabelTemplateDocument
        {
            Version = "1.0",
            Label = new LabelDefinition
            {
                Width = 80,
                Height = 60,
                Gap = 3,
                Density = 10,
                Unit = LabelUnit.Millimeter,
            },
            Elements =
            [
                new TextElement
                {
                    X = 20,
                    Y = 30,
                    Font = "4",
                    Content = "Ceratron",
                },
                new BarcodeElement
                {
                    X = 40,
                    Y = 80,
                    CodeType = BarcodeType.Code39,
                    Height = 60,
                    Readable = false,
                    Narrow = 2,
                    Wide = 4,
                    Content = "N2A5140A0",
                },
                new EraseElement
                {
                    X = 0,
                    Y = 140,
                    Width = 120,
                    Height = 20,
                },
            ],
        };

        var json = serializer.Serialize(template);
        var roundtrip = serializer.Deserialize(json);

        Assert.AreEqual(template.Label.Width, roundtrip.Label.Width);
        Assert.AreEqual(3, roundtrip.Elements.Count);
        Assert.IsInstanceOfType<TextElement>(roundtrip.Elements[0]);
        Assert.IsInstanceOfType<BarcodeElement>(roundtrip.Elements[1]);
        Assert.IsInstanceOfType<EraseElement>(roundtrip.Elements[2]);
        Assert.AreEqual("Ceratron", ((TextElement)roundtrip.Elements[0]).Content);
        Assert.AreEqual("N2A5140A0", ((BarcodeElement)roundtrip.Elements[1]).Content);
        Assert.AreEqual(120, ((EraseElement)roundtrip.Elements[2]).Width);
    }

    /// <summary>
    /// 验证表格单元格内部文本元素在模板快照的序列化往返后仍然存在，避免预览阶段退化成空单元格。
    /// </summary>
    [TestMethod]
    public void SerializeAndDeserialize_ShouldPreserveTableCellInnerTextElements()
    {
        var serializer = new LabelTemplateSerializer();
        var table = new TableElement
        {
            Rows = 1,
            Cols = 1,
            RowHeights = [60],
            ColumnWidths = [120],
            Cells =
            [
                new TableCell
                {
                    ContentType = TableCellContentType.Text,
                    Content = string.Empty,
                    InnerElements =
                    [
                        new TableCellTextElement
                        {
                            X = 6,
                            Y = 6,
                            Width = 48,
                            Height = 28,
                            Content = "文本",
                            Font = "3",
                        },
                    ],
                },
            ],
        };
        var template = new LabelTemplateDocument
        {
            Version = "1.0",
            Label = new LabelDefinition
            {
                Width = 60,
                Height = 40,
                Gap = 2,
                Density = 8,
                Unit = LabelUnit.Millimeter,
            },
            Elements = [table],
        };

        var json = serializer.Serialize(template);
        var roundtrip = serializer.Deserialize(json);

        Assert.AreEqual(1, roundtrip.Elements.Count);
        Assert.IsInstanceOfType<TableElement>(roundtrip.Elements[0]);
        var roundtripTable = (TableElement)roundtrip.Elements[0];
        Assert.AreEqual(1, roundtripTable.Cells.Count);
        Assert.AreEqual(1, roundtripTable.Cells[0].InnerElements.Count);
        Assert.IsInstanceOfType<TableCellTextElement>(roundtripTable.Cells[0].InnerElements[0]);
        Assert.AreEqual("文本", ((TableCellTextElement)roundtripTable.Cells[0].InnerElements[0]).Content);
    }
}
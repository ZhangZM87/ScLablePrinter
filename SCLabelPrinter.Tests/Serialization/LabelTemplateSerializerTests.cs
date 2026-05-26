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
}
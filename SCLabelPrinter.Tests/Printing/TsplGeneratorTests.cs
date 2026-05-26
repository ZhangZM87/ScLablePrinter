using SCLabelPrinter.Core.Models;
using SCLabelPrinter.Core.Printing;

namespace SCLabelPrinter.Tests.Printing;

[TestClass]
public sealed class TsplGeneratorTests
{
    /// <summary>
    /// 验证当外部依赖注入未注册任何写入器时，生成器仍会回退到内置默认写入器。
    /// </summary>
    [TestMethod]
    public void Generate_ShouldFallbackToBuiltInWritersWhenInjectedCollectionIsEmpty()
    {
        var template = new LabelTemplateDocument
        {
            Label = new LabelDefinition(),
            Elements =
            [
                new LineElement
                {
                    X = 0,
                    Y = 100,
                    Width = 240,
                    Height = 4,
                },
            ],
        };

        var generator = new TsplGenerator(Array.Empty<IElementTsplWriter>());

        var command = generator.Generate(template, 1);

        StringAssert.Contains(command, "BAR 0,100,240,4");
    }

    /// <summary>
    /// 验证混合元素可以被正确转换为 TSPL 指令。
    /// </summary>
    [TestMethod]
    public void Generate_ShouldRenderMixedElementsIntoTsplCommands()
    {
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
            Elements =
            [
                new TextElement
                {
                    X = 100,
                    Y = 50,
                    Font = "3",
                    Content = "产品名称",
                },
                new BarcodeElement
                {
                    X = 100,
                    Y = 120,
                    CodeType = BarcodeType.Code128,
                    Height = 80,
                    Readable = true,
                    Narrow = 2,
                    Wide = 2,
                    Content = "12345678",
                },
                new QrCodeElement
                {
                    X = 350,
                    Y = 50,
                    ErrorCorrectionLevel = "L",
                    CellWidth = 5,
                    Mode = "A",
                    Content = "https://example.com",
                },
                new BoxElement
                {
                    X = 50,
                    Y = 30,
                    EndX = 550,
                    EndY = 350,
                    Thickness = 2,
                },
                new LineElement
                {
                    X = 0,
                    Y = 390,
                    Width = 560,
                    Height = 4,
                },
            ],
        };

        var generator = new TsplGenerator();

        var command = generator.Generate(template, 2);

        var expected = string.Join(
            "\r\n",
            "SIZE 60 mm,40 mm",
            "GAP 2 mm",
            "DENSITY 8",
            "CLS",
            "TEXT 100,50,\"3\",0,1,1,\"产品名称\"",
            "BARCODE 100,120,\"128\",80,1,0,2,2,\"12345678\"",
            "QRCODE 350,50,L,5,A,0,\"https://example.com\"",
            "BOX 50,30,550,350,2",
            "BAR 0,390,560,4",
            "PRINT 2",
            string.Empty);

        Assert.AreEqual(expected, command);
    }

    /// <summary>
    /// 验证 ERASE 元素可以生成与 SDK 示例一致的 TSPL 挖空指令。
    /// </summary>
    [TestMethod]
    public void Generate_ShouldRenderEraseElementIntoTsplCommand()
    {
        var template = new LabelTemplateDocument
        {
            Label = new LabelDefinition(),
            Elements =
            [
                new EraseElement
                {
                    X = 110,
                    Y = 1300,
                    Width = 1050,
                    Height = 20,
                },
            ],
        };

        var generator = new TsplGenerator();

        var command = generator.Generate(template, 1);

        StringAssert.Contains(command, "ERASE 110,1300,1050,20");
    }
}
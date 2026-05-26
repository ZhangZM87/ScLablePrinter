using SCLabelPrinter.Core.Models;
using SCLabelPrinter.Core.Printing;
using System.Globalization;
using System.Text;

namespace SCLabelPrinter.Tests.Printing;

[TestClass]
public sealed class TsplParserTests
{
    [TestMethod]
    public void Parse_ShouldConvertHexDumpTsplTextToTemplate()
    {
        var text = "SIZE 60 mm,40 mm\r\nBOX 10,10,200,100,2\r\nTEXT 20,20,\"3\",0,1,1,\"HELLO\"\r\nPRINT 1\r\n";
        var bytes = Encoding.ASCII.GetBytes(text);
        var hexDump = string.Join(' ', bytes.Select(b => b.ToString("X2", CultureInfo.InvariantCulture)));

        var decodedText = TsplTextDecoder.DecodeTextFileBytes(Encoding.ASCII.GetBytes(hexDump));
        Assert.IsTrue(decodedText.Contains("SIZE 60 mm,40 mm"));

        var parser = new TsplParser();
        Assert.IsTrue(parser.TryParse(decodedText, out var template));
        Assert.IsNotNull(template);
        Assert.AreEqual(60, template!.Label.Width);
        Assert.AreEqual(40, template.Label.Height);
        Assert.AreEqual(2, template.Elements.Count);
        Assert.IsInstanceOfType(template.Elements[0], typeof(BoxElement));
        Assert.IsInstanceOfType(template.Elements[1], typeof(TextElement));
    }
}

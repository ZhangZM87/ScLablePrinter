using SCLabelPrinter.Core.Models;
using SCLabelPrinter.Core.Printing;
using System.Globalization;
using System.Linq;
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

    [TestMethod]
    public void DecodeTextFileBytes_ShouldDecodeContiguousHexDump()
    {
        var text = "SIZE 60 mm,40 mm\r\nBOX 10,10,200,100,2\r\nTEXT 20,20,\"3\",0,1,1,\"HELLO\"\r\nPRINT 1\r\n";
        var bytes = Encoding.ASCII.GetBytes(text);
        var hexDump = string.Concat(bytes.Select(b => b.ToString("X2", CultureInfo.InvariantCulture)));

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

    [TestMethod]
    public void Parse_ShouldSupportBitmapElement()
    {
        var text = "SIZE 60 mm,40 mm\r\nBITMAP 10,20,8,5,0,FFFFFFFFFFFFFFFF\r\nPRINT 1\r\n";
        var parser = new TsplParser();

        Assert.IsTrue(parser.TryParse(text, out var template));
        Assert.IsNotNull(template);
        Assert.AreEqual(1, template!.Elements.Count);
        Assert.IsInstanceOfType(template.Elements[0], typeof(BitmapElement));

        var bitmap = (BitmapElement)template.Elements[0];
        Assert.AreEqual(8 * 8, bitmap.Width);
        Assert.AreEqual(5, bitmap.Height);
        Assert.AreEqual(8, bitmap.Data.Length);
    }

    [TestMethod]
    public void Parse_ByteArray_ShouldSupportBitmapElementWithBinaryPayload()
    {
        var header = "SIZE 60 mm,40 mm\r\nBITMAP 10,20,2,4,0,";
        var bitmapBytes = new byte[] { 0xFF, 0x00, 0xAA, 0x55, 0x01, 0x02, 0x03, 0x04 }; // 2*4 = 8 bytes
        var footer = "\r\nPRINT 1\r\n";
        var rawBytes = Encoding.ASCII.GetBytes(header).Concat(bitmapBytes).Concat(Encoding.ASCII.GetBytes(footer)).ToArray();

        var parser = new TsplParser();
        Assert.IsTrue(parser.TryParse(rawBytes, out var template));
        Assert.IsNotNull(template);
        Assert.AreEqual(1, template!.Elements.Count);
        Assert.IsInstanceOfType(template.Elements[0], typeof(BitmapElement));

        var bitmap = (BitmapElement)template.Elements[0];
        Assert.AreEqual(16, bitmap.Width);
        Assert.AreEqual(4, bitmap.Height);
        CollectionAssert.AreEqual(bitmapBytes, bitmap.Data);
    }
}

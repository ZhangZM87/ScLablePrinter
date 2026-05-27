using System.Globalization;
using System.Linq;
using System.Text;
using SCLabelPrinter.Core.Printing;

namespace SCLabelPrinter.Tests.Printing;

/// <summary>
/// 验证打印输入分析器对 TSPL 与二进制数据的识别结果。
/// </summary>
[TestClass]
public sealed class TsplInputAnalyzerTests
{
    /// <summary>
    /// 十六进制转储的 TSPL 文本应当被识别为可解析的 TSPL 指令输入。
    /// </summary>
    [TestMethod]
    public void Analyze_ShouldRecognizeHexDumpTsplPayload()
    {
        var tsplText = "SIZE 60 mm,40 mm\r\nTEXT 20,20,\"3\",0,1,1,\"HELLO\"\r\nPRINT 1\r\n";
        var rawPayload = Encoding.ASCII.GetBytes(tsplText);
        var hexDump = string.Concat(rawPayload.Select(b => b.ToString("X2", CultureInfo.InvariantCulture)));
        var analyzer = new TsplInputAnalyzer();

        var analysis = analyzer.Analyze(Encoding.ASCII.GetBytes(hexDump));

        Assert.AreEqual(PrintInputKind.TsplCommands, analysis.Kind);
        Assert.IsTrue(analysis.IsHexDump);
        Assert.IsTrue(analysis.ShouldRenderGraphicPreview);
        CollectionAssert.AreEqual(rawPayload, analysis.PayloadBytes);
        StringAssert.Contains(analysis.DecodedText, "SIZE 60 mm,40 mm");
    }

    /// <summary>
    /// 对较大 TSPL 十六进制转储仍应正确识别，而不是被长度阈值拒绝。
    /// </summary>
    [TestMethod]
    public void Analyze_ShouldRecognizeLargeHexDumpTsplPayload()
    {
        var builder = new StringBuilder();
        builder.AppendLine("SIZE 40 mm,40 mm");
        for (var index = 0; index < 1200; index++)
        {
            builder.AppendLine($"TEXT 10,10,\"3\",0,1,1,\"HELLO{index}\"");
        }

        builder.AppendLine("PRINT 1");
        var tsplText = builder.ToString();
        var rawPayload = Encoding.ASCII.GetBytes(tsplText);
        var hexDump = string.Concat(rawPayload.Select(b => b.ToString("X2", CultureInfo.InvariantCulture)));
        Assert.IsTrue(hexDump.Length > 40000, "The generated hex dump must exceed the legacy decoder threshold.");

        var analyzer = new TsplInputAnalyzer();
        var analysis = analyzer.Analyze(Encoding.ASCII.GetBytes(hexDump));

        Assert.AreEqual(PrintInputKind.TsplCommands, analysis.Kind);
        Assert.IsTrue(analysis.IsHexDump);
        Assert.IsTrue(analysis.ShouldRenderGraphicPreview);
        StringAssert.Contains(analysis.DecodedText, "SIZE 40 mm,40 mm");
    }

    /// <summary>
    /// 控制字符占比明显过高的载荷应当回退为二进制打印包，而不是错误按文本解析。
    /// </summary>
    [TestMethod]
    public void Analyze_ShouldFallbackToBinaryForControlHeavyPayload()
    {
        var payload = new byte[] { 0x1B, 0x40, 0x00, 0x02, 0xFF, 0x10, 0x33, 0x80, 0x04 };
        var analyzer = new TsplInputAnalyzer();

        var analysis = analyzer.Analyze(payload);

        Assert.AreEqual(PrintInputKind.Binary, analysis.Kind);
        Assert.IsFalse(analysis.ShouldRenderGraphicPreview);
        CollectionAssert.AreEqual(payload, analysis.PayloadBytes);
    }

    [TestMethod]
    public void Analyze_ShouldRecognizeRawTsplBytePayloadWithBitmap()
    {
        var header = "SIZE 60 mm,40 mm\r\nBITMAP 10,20,2,4,0,";
        var bitmapBytes = new byte[] { 0xFF, 0x00, 0xAA, 0x55, 0x11, 0x22, 0x33, 0x44 };
        var footer = "\r\nPRINT 1\r\n";
        var rawBytes = Encoding.ASCII.GetBytes(header).Concat(bitmapBytes).Concat(Encoding.ASCII.GetBytes(footer)).ToArray();
        var analyzer = new TsplInputAnalyzer();

        var analysis = analyzer.Analyze(rawBytes);

        Assert.AreEqual(PrintInputKind.TsplCommands, analysis.Kind);
        Assert.IsFalse(analysis.IsHexDump);
        Assert.IsTrue(analysis.ShouldRenderGraphicPreview);
        StringAssert.Contains(analysis.DecodedText, "SIZE 60 mm");
        StringAssert.Contains(analysis.DecodedText, "BITMAP 10,20,2,4,0,");
        CollectionAssert.AreEqual(rawBytes, analysis.PayloadBytes);
    }
}
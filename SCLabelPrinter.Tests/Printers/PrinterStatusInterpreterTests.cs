using SCLabelPrinter.Core.Printers;

namespace SCLabelPrinter.Tests.Printers;

[TestClass]
public sealed class PrinterStatusInterpreterTests
{
    [TestMethod]
    public void InterpretTsplStatus_ShouldMapKnownCodes()
    {
        var interpreter = new PrinterStatusInterpreter();

        var ready = interpreter.InterpretTsplStatus(0x00);
        var outOfPaper = interpreter.InterpretTsplStatus(0x04);
        var overheated = interpreter.InterpretTsplStatus(0x80);

        Assert.AreEqual(PrinterState.Ready, ready.State);
        Assert.AreEqual("正常待机", ready.Description);
        Assert.AreEqual(PrinterState.OutOfPaper, outOfPaper.State);
        Assert.AreEqual("缺纸", outOfPaper.Description);
        Assert.AreEqual(PrinterState.Overheated, overheated.State);
        Assert.AreEqual("打印头过热", overheated.Description);
    }

    [TestMethod]
    public void InterpretTsplStatus_ShouldFallbackToUnknownForUnsupportedCode()
    {
        var interpreter = new PrinterStatusInterpreter();

        var status = interpreter.InterpretTsplStatus(0x7F);

        Assert.AreEqual(PrinterState.Unknown, status.State);
        Assert.AreEqual("未知", status.Description);
    }
}
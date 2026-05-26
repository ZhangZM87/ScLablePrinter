using SCLabelPrinter.Core.Printing;

namespace SCLabelPrinter.Tests.Printing;

[TestClass]
public sealed class PrintDataChunkerTests
{
    [TestMethod]
    public void Split_ShouldChunkBinaryPayloadByConfiguredPacketSize()
    {
        var payload = Enumerable.Range(0, 7000)
            .Select(index => (byte)(index % 251))
            .ToArray();

        var chunker = new PrintDataChunker();

        var packets = chunker.Split(payload, 3072).ToArray();

        Assert.AreEqual(3, packets.Length);
        Assert.AreEqual(3072, packets[0].Length);
        Assert.AreEqual(3072, packets[1].Length);
        Assert.AreEqual(856, packets[2].Length);
        CollectionAssert.AreEqual(payload[..3072], packets[0]);
        CollectionAssert.AreEqual(payload[3072..6144], packets[1]);
        CollectionAssert.AreEqual(payload[6144..], packets[2]);
    }
}
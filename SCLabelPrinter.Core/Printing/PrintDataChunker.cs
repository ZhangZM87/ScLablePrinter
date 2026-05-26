namespace SCLabelPrinter.Core.Printing;

/// <summary>
/// 负责将打印数据按固定大小拆分为多个数据包，便于稳定发送到打印机。
/// </summary>
public sealed class PrintDataChunker
{
    /// <summary>
    /// 将完整打印数据拆分为多个数据包。
    /// </summary>
    public IReadOnlyList<byte[]> Split(byte[] payload, int packetSize)
    {
        ArgumentNullException.ThrowIfNull(payload);

        if (packetSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(packetSize), packetSize, "分包大小必须大于 0。");
        }

        if (payload.Length == 0)
        {
            return [];
        }

        var packets = new List<byte[]>();
        for (var offset = 0; offset < payload.Length; offset += packetSize)
        {
            var currentSize = Math.Min(packetSize, payload.Length - offset);
            var packet = new byte[currentSize];
            Array.Copy(payload, offset, packet, 0, currentSize);
            packets.Add(packet);
        }

        return packets;
    }
}
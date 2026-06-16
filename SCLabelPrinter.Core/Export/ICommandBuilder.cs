namespace SCLabelPrinter.Core.Export;

/// <summary>
/// 打印机指令构建器抽象，支持文本指令（TSPL/ZPL/CPCL）和二进制指令（ESC/POS）两种模式。
/// 各打印机语言的 Exporter 通过此接口组装完整的打印指令流。
/// </summary>
public interface ICommandBuilder
{
    /// <summary>
    /// 追加一行文本指令（适用于 TSPL、ZPL、CPCL 等文本协议）。
    /// </summary>
    void AppendLine(string command);

    /// <summary>
    /// 追加多行文本指令。
    /// </summary>
    void AppendLines(IEnumerable<string> commands);

    /// <summary>
    /// 追加原始字节数据（适用于 ESC/POS 等二进制协议）。
    /// </summary>
    void AppendBytes(byte[] data);

    /// <summary>
    /// 获取已构建的完整指令，以字节数组形式返回。
    /// </summary>
    byte[] ToByteArray();

    /// <summary>
    /// 获取已构建的完整指令，以文本形式返回（仅适用于文本协议）。
    /// </summary>
    string ToText();
}
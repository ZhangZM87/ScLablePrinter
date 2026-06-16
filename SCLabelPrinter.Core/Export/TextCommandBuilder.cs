using System.Text;

namespace SCLabelPrinter.Core.Export;

/// <summary>
/// 文本指令构建器，适用于 TSPL、ZPL、CPCL 等基于文本行的打印机协议。
/// </summary>
public sealed class TextCommandBuilder : ICommandBuilder
{
    private readonly List<string> _lines = new();

    /// <summary>
    /// 追加一行文本指令。
    /// </summary>
    public void AppendLine(string command)
    {
        _lines.Add(command);
    }

    /// <summary>
    /// 追加多行文本指令。
    /// </summary>
    public void AppendLines(IEnumerable<string> commands)
    {
        _lines.AddRange(commands);
    }

    /// <summary>
    /// 追加原始字节（文本协议下将按 GB2312 解码后追加为一行）。
    /// </summary>
    public void AppendBytes(byte[] data)
    {
        _lines.Add(Encoding.GetEncoding("GB2312").GetString(data));
    }

    /// <summary>
    /// 返回所有指令的文本表示，使用 CRLF 换行。
    /// </summary>
    public string ToText()
    {
        return string.Join("\r\n", _lines) + "\r\n";
    }

    /// <summary>
    /// 返回所有指令的字节数组表示（GB2312 编码）。
    /// </summary>
    public byte[] ToByteArray()
    {
        return Encoding.GetEncoding("GB2312").GetBytes(ToText());
    }
}
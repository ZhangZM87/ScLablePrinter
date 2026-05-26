using SCLabelPrinter.Core.Models;

namespace SCLabelPrinter.Core.Printing;

/// <summary>
/// 定义单个标签元素到 TSPL 指令的转换接口，便于未来增加新的元素类型。
/// </summary>
public interface IElementTsplWriter
{
    /// <summary>
    /// 判断当前写入器是否支持指定的标签元素。
    /// </summary>
    bool CanWrite(LabelElement element);

    /// <summary>
    /// 将标签元素转换为一条 TSPL 指令。
    /// </summary>
    string Write(LabelElement element);
}
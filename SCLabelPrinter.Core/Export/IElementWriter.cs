using SCLabelPrinter.Core.Models;

namespace SCLabelPrinter.Core.Export;

/// <summary>
/// 定义单个标签元素到打印机指令的转换接口。
/// 每种打印机语言的每种元素类型各有一个实现，
/// 新增元素类型或打印机语言只需添加新实现并注册到 DI 容器。
/// </summary>
public interface IElementWriter
{
    /// <summary>
    /// 当前写入器对应的打印机语言。
    /// </summary>
    PrinterLanguage Language { get; }

    /// <summary>
    /// 判断当前写入器是否支持指定的标签元素。
    /// </summary>
    bool CanWrite(LabelElement element);

    /// <summary>
    /// 将标签元素转换为打印机指令并写入命令构建器。
    /// </summary>
    void Write(LabelElement element, ICommandBuilder builder);
}
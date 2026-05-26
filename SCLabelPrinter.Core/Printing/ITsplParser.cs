using SCLabelPrinter.Core.Models;

namespace SCLabelPrinter.Core.Printing;

/// <summary>
/// 定义从 TSPL 文本解析为标签模板模型的契约。
/// </summary>
public interface ITsplParser
{
    /// <summary>
    /// 尝试解析 TSPL 文本为标签模板文档。
    /// </summary>
    bool TryParse(string tsplText, out LabelTemplateDocument? template);

    /// <summary>
    /// 解析 TSPL 文本为标签模板文档，无法解析时抛出异常。
    /// </summary>
    LabelTemplateDocument Parse(string tsplText);
}

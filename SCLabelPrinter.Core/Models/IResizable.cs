namespace SCLabelPrinter.Core.Models;

/// <summary>
/// 可调整大小的元素接口。
/// 所有支持在画布上通过拖拽手柄调整尺寸的元素都应实现此接口。
/// </summary>
public interface IResizable
{
    /// <summary>
    /// 获取或设置元素的宽度。
    /// </summary>
    int ElementWidth { get; set; }

    /// <summary>
    /// 获取或设置元素的高度。
    /// </summary>
    int ElementHeight { get; set; }

    /// <summary>
    /// 获取元素允许的最小宽度。
    /// </summary>
    int MinWidth { get; }

    /// <summary>
    /// 获取元素允许的最小高度。
    /// </summary>
    int MinHeight { get; }
}

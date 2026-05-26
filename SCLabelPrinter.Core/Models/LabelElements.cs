using System.Text.Json.Serialization;

namespace SCLabelPrinter.Core.Models;

/// <summary>
/// 定义标签元素的公共位置和旋转属性，支持未来扩展更多元素类型。
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(TextElement), "text")]
[JsonDerivedType(typeof(BarcodeElement), "barcode")]
[JsonDerivedType(typeof(QrCodeElement), "qrcode")]
[JsonDerivedType(typeof(BoxElement), "box")]
[JsonDerivedType(typeof(LineElement), "line")]
[JsonDerivedType(typeof(EraseElement), "erase")]
public abstract class LabelElement
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public int X { get; set; }

    public int Y { get; set; }

    public int Rotation { get; set; }
}

/// <summary>
/// 表示文本元素。
/// </summary>
public sealed class TextElement : LabelElement
{
    public string Font { get; set; } = "3";

    public int XScale { get; set; } = 1;

    public int YScale { get; set; } = 1;

    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 返回适合界面列表展示的文本元素摘要。
    /// </summary>
    public override string ToString()
    {
        return $"文本: {Content}";
    }
}

/// <summary>
/// 表示一维条码元素。
/// </summary>
public sealed class BarcodeElement : LabelElement
{
    public BarcodeType CodeType { get; set; } = BarcodeType.Code128;

    public int Height { get; set; } = 80;

    public bool Readable { get; set; } = true;

    public int Narrow { get; set; } = 2;

    public int Wide { get; set; } = 2;

    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 返回适合界面列表展示的条码元素摘要。
    /// </summary>
    public override string ToString()
    {
        return $"条码: {Content}";
    }
}

/// <summary>
/// 表示二维码元素。
/// </summary>
public sealed class QrCodeElement : LabelElement
{
    public string ErrorCorrectionLevel { get; set; } = "L";

    public int CellWidth { get; set; } = 5;

    public string Mode { get; set; } = "A";

    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 返回适合界面列表展示的二维码元素摘要。
    /// </summary>
    public override string ToString()
    {
        return $"二维码: {Content}";
    }
}

/// <summary>
/// 表示矩形框元素。
/// </summary>
public sealed class BoxElement : LabelElement
{
    public int EndX { get; set; }

    public int EndY { get; set; }

    public int Thickness { get; set; } = 2;

    /// <summary>
    /// 返回适合界面列表展示的矩形框元素摘要。
    /// </summary>
    public override string ToString()
    {
        return $"矩形框: ({X},{Y})-({EndX},{EndY})";
    }
}

/// <summary>
/// 表示线条元素。
/// </summary>
public sealed class LineElement : LabelElement
{
    public int Width { get; set; }

    public int Height { get; set; } = 2;

    /// <summary>
    /// 返回适合界面列表展示的线条元素摘要。
    /// </summary>
    public override string ToString()
    {
        return $"线条: {Width}x{Height}";
    }
}

/// <summary>
/// 表示挖空区域元素，对应 TSPL ERASE 指令。
/// </summary>
public sealed class EraseElement : LabelElement
{
    public int Width { get; set; }

    public int Height { get; set; }

    /// <summary>
    /// 返回适合界面列表展示的挖空元素摘要。
    /// </summary>
    public override string ToString()
    {
        return $"挖空: {Width}x{Height}";
    }
}

/// <summary>
/// 视图层通过画布拖动时传递的元素移动请求。
/// </summary>
public sealed record ElementMoveRequest(string ElementId, int X, int Y);

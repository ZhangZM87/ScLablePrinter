using System.Text.Json.Serialization;

namespace SCLabelPrinter.Core.Models;

/// <summary>
/// 表示完整的标签模板文档，用于编辑、预览、保存和打印。
/// </summary>
public sealed class LabelTemplateDocument
{
    public string Version { get; set; } = "1.0";

    public LabelDefinition Label { get; set; } = new();

    public List<LabelElement> Elements { get; set; } = [];
}

/// <summary>
/// 表示标签画布的尺寸和打印参数。
/// </summary>
public sealed class LabelDefinition
{
    public double Width { get; set; } = 60;

    public double Height { get; set; } = 40;

    public double Gap { get; set; } = 2;

    public int Density { get; set; } = 8;

    public LabelUnit Unit { get; set; } = LabelUnit.Millimeter;
}

/// <summary>
/// 表示标签尺寸使用的单位。
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum LabelUnit
{
    Millimeter,
    Dot,
}

/// <summary>
/// 表示条码使用的制式。
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum BarcodeType
{
    Code39,
    Code128,
    Ean13,
}
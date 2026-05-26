using System.Text.Json;
using System.Text.Json.Serialization;
using SCLabelPrinter.Core.Models;

namespace SCLabelPrinter.Core.Serialization;

/// <summary>
/// 负责将标签模板在对象模型与 JSON 文本之间进行转换。
/// </summary>
public sealed class LabelTemplateSerializer
{
    private readonly JsonSerializerOptions _options = CreateOptions();

    /// <summary>
    /// 将标签模板序列化为 JSON 字符串。
    /// </summary>
    public string Serialize(LabelTemplateDocument template)
    {
        ArgumentNullException.ThrowIfNull(template);
        return JsonSerializer.Serialize(template, _options);
    }

    /// <summary>
    /// 将 JSON 字符串反序列化为标签模板对象。
    /// </summary>
    public LabelTemplateDocument Deserialize(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);

        return JsonSerializer.Deserialize<LabelTemplateDocument>(json, _options)
            ?? throw new InvalidOperationException("标签模板内容为空或格式不正确。");
    }

    /// <summary>
    /// 创建统一的 JSON 序列化选项，保证文件结构稳定且可扩展。
    /// </summary>
    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        return options;
    }
}
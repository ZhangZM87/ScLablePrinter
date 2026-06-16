namespace SCLabelPrinter.Core.Services;

/// <summary>
/// Excel 导入服务接口，用于从 Excel 文件读取数据并填充到表格元素。
/// </summary>
public interface IExcelImportService
{
    /// <summary>
    /// 从 Excel 文件导入数据。
    /// </summary>
    /// <param name="filePath">Excel 文件路径（.xlsx）</param>
    /// <param name="sheetName">工作表名称，为 null 时读取第一个工作表</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>导入结果，包含表头和数据行</returns>
    Task<ExcelImportResult> ImportAsync(string filePath, string? sheetName = null, CancellationToken ct = default);

    /// <summary>
    /// 获取 Excel 文件中所有工作表名称。
    /// </summary>
    Task<IReadOnlyList<string>> GetSheetNamesAsync(string filePath, CancellationToken ct = default);
}

/// <summary>
/// Excel 导入结果，包含表头和数据行列表。
/// </summary>
public sealed class ExcelImportResult
{
    /// <summary>
    /// 列标题（第一行）。
    /// </summary>
    public List<string> Headers { get; set; } = [];

    /// <summary>
    /// 数据行（从第二行开始），每行是一个字符串列表。
    /// </summary>
    public List<List<string>> Rows { get; set; } = [];

    /// <summary>
    /// 导入的总行数（不含表头）。
    /// </summary>
    public int TotalRows => Rows.Count;

    /// <summary>
    /// 导入的总列数。
    /// </summary>
    public int TotalColumns => Headers.Count;
}

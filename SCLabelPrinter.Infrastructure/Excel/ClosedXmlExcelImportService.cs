using ClosedXML.Excel;
using SCLabelPrinter.Core.Services;

namespace SCLabelPrinter.Infrastructure.Excel;

/// <summary>
/// 基于 ClosedXML 的 Excel 导入服务实现。
/// 支持读取 .xlsx 文件中的工作表数据并转换为表格可用的行列结构。
/// </summary>
public sealed class ClosedXmlExcelImportService : IExcelImportService
{
    /// <summary>
    /// 从指定 Excel 文件导入数据。第一行作为表头，其余行作为数据。
    /// </summary>
    public Task<ExcelImportResult> ImportAsync(string filePath, string? sheetName = null, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        var tempPath = Path.GetTempFileName() + Path.GetExtension(filePath);
        File.Copy(filePath, tempPath, true);
        try
        {
        using var workbook = new XLWorkbook(tempPath);
        var worksheet = sheetName is not null
            ? workbook.Worksheet(sheetName)
            : workbook.Worksheets.First();

        var usedRange = worksheet.RangeUsed();
        if (usedRange is null)
        {
            return Task.FromResult(new ExcelImportResult());
        }

        var rowCount = usedRange.RowCount();
        var colCount = usedRange.ColumnCount();

        var result = new ExcelImportResult();

        // 第一行作为表头
        for (var col = 1; col <= colCount; col++)
        {
            result.Headers.Add(worksheet.Cell(1, col).GetString().Trim());
        }

        // 其余行作为数据
        for (var row = 2; row <= rowCount; row++)
        {
            ct.ThrowIfCancellationRequested();
            var rowData = new List<string>();
            for (var col = 1; col <= colCount; col++)
            {
                rowData.Add(worksheet.Cell(row, col).GetString().Trim());
            }
            result.Rows.Add(rowData);
        }

        return Task.FromResult(result);
        }
        finally
        {
            try { File.Delete(tempPath); } catch { }
        }
    }

    /// <summary>
    /// 获取 Excel 文件中所有工作表的名称列表。
    /// </summary>
    public Task<IReadOnlyList<string>> GetSheetNamesAsync(string filePath, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        var tempPath = Path.GetTempFileName() + Path.GetExtension(filePath);
        File.Copy(filePath, tempPath, true);
        try
        {
            using var workbook = new XLWorkbook(tempPath);
            var names = workbook.Worksheets.Select(ws => ws.Name).ToList();
            return Task.FromResult<IReadOnlyList<string>>(names);
        }
        finally
        {
            try { File.Delete(tempPath); } catch { }
        }
    }
}

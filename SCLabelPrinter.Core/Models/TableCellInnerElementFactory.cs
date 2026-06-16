namespace SCLabelPrinter.Core.Models;

/// <summary>
/// 负责为表格单元格创建具备合理初始尺寸的内部元素，避免 UI 层继续散落硬编码默认值。
/// </summary>
public static class TableCellInnerElementFactory
{
    private const int DefaultPadding = 6;
    private const int TextContentPadding = 2;
    private static readonly ITableCellTextPreviewMetricsService TextPreviewMetricsService = new TableCellTextPreviewMetricsService();

    /// <summary>
    /// 创建适配当前单元格可用空间的文本内部元素。
    /// </summary>
    public static TableCellTextElement CreateTextElement(TableElement table, int rowIndex, int columnIndex, string content)
    {
        var element = new TableCellTextElement
        {
            Content = content,
            Width = 48,
            Height = TextPreviewMetricsService.GetMinimumFrameHeight("3", 1, TextContentPadding),
        };
        TableCellLayoutCalculator.FitInnerElementToCell(table, rowIndex, columnIndex, element, DefaultPadding);
        return element;
    }

    /// <summary>
    /// 创建适配当前单元格可用空间的条码内部元素。
    /// </summary>
    public static TableCellBarcodeElement CreateBarcodeElement(TableElement table, int rowIndex, int columnIndex, string content)
    {
        var element = new TableCellBarcodeElement
        {
            Content = content,
            Width = 140,
            Height = 40,
        };
        TableCellLayoutCalculator.FitInnerElementToCell(table, rowIndex, columnIndex, element, DefaultPadding);
        return element;
    }

    /// <summary>
    /// 创建适配当前单元格可用空间的二维码内部元素。
    /// </summary>
    public static TableCellQrCodeElement CreateQrCodeElement(TableElement table, int rowIndex, int columnIndex, string content)
    {
        var element = new TableCellQrCodeElement
        {
            Content = content,
            Width = 80,
            Height = 80,
        };
        TableCellLayoutCalculator.FitInnerElementToCell(table, rowIndex, columnIndex, element, DefaultPadding);
        return element;
    }
}
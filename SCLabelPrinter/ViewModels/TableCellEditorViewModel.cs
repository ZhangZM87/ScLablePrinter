using CommunityToolkit.Mvvm.ComponentModel;
using SCLabelPrinter.Core.Models;

namespace SCLabelPrinter.ViewModels;

/// <summary>
/// 提供表格单元格编辑对话框的数据和行为。
/// </summary>
public sealed class TableCellEditorViewModel : ObservableObject
{
    /// <summary>
    /// 初始化表格单元格编辑视图模型。
    /// </summary>
    public TableCellEditorViewModel(TableCell cell)
    {
        ContentType = cell.ContentType;
        Content = cell.Content;
        BarcodeType = cell.BarcodeType;
        QrCellWidth = cell.QrCellWidth;
        QrErrorCorrectionLevel = cell.QrErrorCorrectionLevel;
        QrMode = cell.QrMode;
    }

    public TableCellContentType ContentType { get; set; }

    public string Content { get; set; } = string.Empty;

    public BarcodeType BarcodeType { get; set; } = BarcodeType.Code128;

    public int QrCellWidth { get; set; } = 5;

    public string QrErrorCorrectionLevel { get; set; } = "L";

    public string QrMode { get; set; } = "A";

    /// <summary>
    /// 生成当前编辑状态下的表格单元格对象。
    /// </summary>
    public TableCell BuildTableCell()
    {
        return new TableCell
        {
            ContentType = ContentType,
            Content = Content,
            BarcodeType = BarcodeType,
            QrCellWidth = QrCellWidth,
            QrErrorCorrectionLevel = QrErrorCorrectionLevel,
            QrMode = QrMode,
        };
    }
}

using System.Data;
using System.Linq;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SCLabelPrinter.Core.Models;
using SCLabelPrinter.Views;

namespace SCLabelPrinter.ViewModels;

/// <summary>
/// 提供类似电子表格的表格结构与单元格编辑能力，并将修改隔离在工作副本中直到用户确认。
/// </summary>
public sealed partial class TableSpreadsheetEditorViewModel : ObservableObject
{
    private readonly TableElement _workingTable;

    /// <summary>
    /// 基于当前选中的表格元素创建一个独立的编辑工作副本。
    /// </summary>
    public TableSpreadsheetEditorViewModel(TableElement table)
    {
        ArgumentNullException.ThrowIfNull(table);

        _workingTable = CloneTable(table);
        RebuildSpreadsheet();
    }

    [ObservableProperty]
    private DataTable spreadsheet = new();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RemoveSelectedRowCommand))]
    [NotifyCanExecuteChangedFor(nameof(RemoveSelectedColumnCommand))]
    [NotifyCanExecuteChangedFor(nameof(ApplySelectedCellChangesCommand))]
    [NotifyCanExecuteChangedFor(nameof(EditSelectedCellInnerElementsCommand))]
    private int selectedRowIndex = -1;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RemoveSelectedRowCommand))]
    [NotifyCanExecuteChangedFor(nameof(RemoveSelectedColumnCommand))]
    [NotifyCanExecuteChangedFor(nameof(ApplySelectedCellChangesCommand))]
    [NotifyCanExecuteChangedFor(nameof(EditSelectedCellInnerElementsCommand))]
    private int selectedColumnIndex = -1;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ApplySelectedCellChangesCommand))]
    private bool selectedCellEditable;

    [ObservableProperty]
    private string selectedCellAddress = "未选择单元格";

    [ObservableProperty]
    private string selectedCellContent = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ApplySelectedCellChangesCommand))]
    private TableCellContentType selectedCellContentType = TableCellContentType.Text;

    [ObservableProperty]
    private LabelTextAlignment selectedCellAlignment = LabelTextAlignment.Left;

    [ObservableProperty]
    private string selectedCellHint = "请选择一个单元格开始编辑。";

    [ObservableProperty]
    private string structureSummary = string.Empty;

    /// <summary>
    /// 提供给 DataGrid 绑定的视图数据。
    /// </summary>
    public DataView SpreadsheetView => Spreadsheet.DefaultView;

    /// <summary>
    /// 当工作表数据对象替换时，同步刷新 DataGrid 绑定视图。
    /// </summary>
    partial void OnSpreadsheetChanged(DataTable value)
    {
        OnPropertyChanged(nameof(SpreadsheetView));
    }

    /// <summary>
    /// 选中指定单元格并同步右侧编辑区字段。
    /// </summary>
    public void SelectCell(int rowIndex, int columnIndex)
    {
        if (!IsCellIndexInRange(rowIndex, columnIndex))
        {
            return;
        }

        SelectedRowIndex = rowIndex;
        SelectedColumnIndex = columnIndex;
        LoadSelectedCellEditor();
    }

    /// <summary>
    /// 判断指定单元格是否允许在网格中直接键入编辑。
    /// </summary>
    public bool CanEditGridCell(int rowIndex, int columnIndex)
    {
        return TryGetCell(rowIndex, columnIndex, out var cell) && cell.InnerElements.Count == 0;
    }

    /// <summary>
    /// 将 DataGrid 内的直接输入同步到工作副本中的对应单元格。
    /// </summary>
    public void UpdateGridCellText(int rowIndex, int columnIndex, string? value)
    {
        if (!CanEditGridCell(rowIndex, columnIndex) || !TryGetCell(rowIndex, columnIndex, out var cell))
        {
            return;
        }

        cell.Content = value ?? string.Empty;
        Spreadsheet.Rows[rowIndex][columnIndex] = GetCellDisplayValue(cell);

        if (SelectedRowIndex == rowIndex && SelectedColumnIndex == columnIndex)
        {
            LoadSelectedCellEditor();
        }
    }

    /// <summary>
    /// 构建确认后的表格结果，供主编辑器回写到真实元素实例。
    /// </summary>
    public TableElement BuildTable()
    {
        return CloneTable(_workingTable);
    }

    /// <summary>
    /// 在当前行上方插入一行，保持与 TableElement 自身的结构维护规则一致。
    /// </summary>
    [RelayCommand]
    private void AddRowAbove()
    {
        var rowIndex = SelectedRowIndex >= 0 ? SelectedRowIndex : 0;
        _workingTable.InsertRowAt(rowIndex);
        RebuildSpreadsheet(rowIndex, Math.Max(SelectedColumnIndex, 0));
    }

    /// <summary>
    /// 在当前行下方插入一行，便于快速扩展表格结构。
    /// </summary>
    [RelayCommand]
    private void AddRowBelow()
    {
        var rowIndex = SelectedRowIndex >= 0 ? SelectedRowIndex + 1 : _workingTable.Rows;
        _workingTable.InsertRowAt(rowIndex);
        RebuildSpreadsheet(rowIndex, Math.Max(SelectedColumnIndex, 0));
    }

    /// <summary>
    /// 删除当前选中行，至少保留一行以避免生成非法表格结构。
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanRemoveSelectedRow))]
    private void RemoveSelectedRow()
    {
        if (SelectedRowIndex < 0)
        {
            return;
        }

        var targetColumn = Math.Max(0, SelectedColumnIndex);
        var nextRow = Math.Max(0, SelectedRowIndex - 1);
        _workingTable.RemoveRowAt(SelectedRowIndex);
        RebuildSpreadsheet(nextRow, targetColumn);
    }

    /// <summary>
    /// 在当前列左侧插入一列，保持电子表格式的结构操作顺序。
    /// </summary>
    [RelayCommand]
    private void AddColumnLeft()
    {
        var columnIndex = SelectedColumnIndex >= 0 ? SelectedColumnIndex : 0;
        _workingTable.InsertColumnAt(columnIndex);
        RebuildSpreadsheet(Math.Max(SelectedRowIndex, 0), columnIndex);
    }

    /// <summary>
    /// 在当前列右侧插入一列，便于连续扩展表格。
    /// </summary>
    [RelayCommand]
    private void AddColumnRight()
    {
        var columnIndex = SelectedColumnIndex >= 0 ? SelectedColumnIndex + 1 : _workingTable.Cols;
        _workingTable.InsertColumnAt(columnIndex);
        RebuildSpreadsheet(Math.Max(SelectedRowIndex, 0), columnIndex);
    }

    /// <summary>
    /// 删除当前选中列，至少保留一列以避免生成非法表格结构。
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanRemoveSelectedColumn))]
    private void RemoveSelectedColumn()
    {
        if (SelectedColumnIndex < 0)
        {
            return;
        }

        var targetRow = Math.Max(0, SelectedRowIndex);
        var nextColumn = Math.Max(0, SelectedColumnIndex - 1);
        _workingTable.RemoveColumnAt(SelectedColumnIndex);
        RebuildSpreadsheet(targetRow, nextColumn);
    }

    /// <summary>
    /// 将右侧编辑区中的内容与类型应用到当前选中单元格。
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanApplySelectedCellChanges))]
    private void ApplySelectedCellChanges()
    {
        if (!TryGetCell(SelectedRowIndex, SelectedColumnIndex, out var cell))
        {
            return;
        }

        cell.ContentType = SelectedCellContentType;
        cell.Content = SelectedCellContent ?? string.Empty;
        cell.Alignment = SelectedCellAlignment;
        Spreadsheet.Rows[SelectedRowIndex][SelectedColumnIndex] = GetCellDisplayValue(cell);
        LoadSelectedCellEditor();
    }

    /// <summary>
    /// 打开当前单元格的内部元素编辑器，用于处理条码、二维码和复杂文本布局。
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanEditSelectedCellInnerElements))]
    private void EditSelectedCellInnerElements()
    {
        if (!TryGetCell(SelectedRowIndex, SelectedColumnIndex, out var cell))
        {
            return;
        }

        var editor = new TableCellInnerElementEditorViewModel(cell);
        var dialog = new TableCellInnerElementEditorWindow
        {
            DataContext = editor,
            Owner = Application.Current?.MainWindow,
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        var cellIndex = SelectedRowIndex * _workingTable.Cols + SelectedColumnIndex;
        _workingTable.Cells[cellIndex] = editor.BuildTableCell();
        Spreadsheet.Rows[SelectedRowIndex][SelectedColumnIndex] = GetCellDisplayValue(_workingTable.Cells[cellIndex]);
        LoadSelectedCellEditor();
    }

    /// <summary>
    /// 判断当前是否允许删除选中行。
    /// </summary>
    private bool CanRemoveSelectedRow()
    {
        return SelectedRowIndex >= 0 && _workingTable.Rows > 1;
    }

    /// <summary>
    /// 判断当前是否允许删除选中列。
    /// </summary>
    private bool CanRemoveSelectedColumn()
    {
        return SelectedColumnIndex >= 0 && _workingTable.Cols > 1;
    }

    /// <summary>
    /// 判断当前选中单元格是否允许直接应用内容编辑。
    /// </summary>
    private bool CanApplySelectedCellChanges()
    {
        return SelectedCellEditable && TryGetCell(SelectedRowIndex, SelectedColumnIndex, out _);
    }

    /// <summary>
    /// 判断当前是否存在可编辑内部元素的选中单元格。
    /// </summary>
    private bool CanEditSelectedCellInnerElements()
    {
        return TryGetCell(SelectedRowIndex, SelectedColumnIndex, out _);
    }

    /// <summary>
    /// 重新生成 DataGrid 视图数据，并尽量保留当前聚焦位置。
    /// </summary>
    private void RebuildSpreadsheet(int preferredRowIndex = 0, int preferredColumnIndex = 0)
    {
        _workingTable.EnsureCellCount();
        var table = new DataTable();

        for (var columnIndex = 0; columnIndex < _workingTable.Cols; columnIndex++)
        {
            table.Columns.Add(GetSpreadsheetColumnName(columnIndex), typeof(string));
        }

        for (var rowIndex = 0; rowIndex < _workingTable.Rows; rowIndex++)
        {
            var row = table.NewRow();
            for (var columnIndex = 0; columnIndex < _workingTable.Cols; columnIndex++)
            {
                row[columnIndex] = GetCellDisplayValue(GetCell(rowIndex, columnIndex));
            }

            table.Rows.Add(row);
        }

        Spreadsheet = table;
        StructureSummary = $"{_workingTable.Rows} 行 x {_workingTable.Cols} 列";

        if (_workingTable.Rows == 0 || _workingTable.Cols == 0)
        {
            SelectedRowIndex = -1;
            SelectedColumnIndex = -1;
            SelectedCellEditable = false;
            SelectedCellAddress = "未选择单元格";
            SelectedCellContent = string.Empty;
            SelectedCellHint = "当前表格没有可编辑单元格。";
            return;
        }

        var targetRowIndex = Math.Clamp(preferredRowIndex, 0, _workingTable.Rows - 1);
        var targetColumnIndex = Math.Clamp(preferredColumnIndex, 0, _workingTable.Cols - 1);
        SelectCell(targetRowIndex, targetColumnIndex);
    }

    /// <summary>
    /// 将当前选中单元格的状态同步到右侧编辑面板。
    /// </summary>
    private void LoadSelectedCellEditor()
    {
        if (!TryGetCell(SelectedRowIndex, SelectedColumnIndex, out var cell))
        {
            SelectedCellEditable = false;
            SelectedCellAddress = "未选择单元格";
            SelectedCellContent = string.Empty;
            SelectedCellHint = "请选择一个单元格开始编辑。";
            return;
        }

        SelectedCellAddress = $"{GetSpreadsheetColumnName(SelectedColumnIndex)}{SelectedRowIndex + 1}";
        SelectedCellContentType = cell.ContentType;
        SelectedCellContent = cell.Content;
        SelectedCellAlignment = cell.Alignment;
        SelectedCellEditable = cell.InnerElements.Count == 0;
        SelectedCellHint = cell.InnerElements.Count > 0
            ? $"当前单元格包含 {cell.InnerElements.Count} 个内部元素，请使用“编辑内部元素”进行精细编辑。"
            : "当前单元格支持像电子表格一样直接录入内容。";
    }

    /// <summary>
    /// 尝试获取指定位置的单元格，统一处理越界保护和线性索引换算。
    /// </summary>
    private bool TryGetCell(int rowIndex, int columnIndex, out TableCell cell)
    {
        cell = null!;
        if (!IsCellIndexInRange(rowIndex, columnIndex))
        {
            return false;
        }

        var cellIndex = rowIndex * _workingTable.Cols + columnIndex;
        if (cellIndex < 0 || cellIndex >= _workingTable.Cells.Count)
        {
            return false;
        }

        cell = _workingTable.Cells[cellIndex];
        return true;
    }

    /// <summary>
    /// 获取指定位置单元格，供内部重建视图时在索引已验证的情况下直接读取。
    /// </summary>
    private TableCell GetCell(int rowIndex, int columnIndex)
    {
        return _workingTable.Cells[rowIndex * _workingTable.Cols + columnIndex];
    }

    /// <summary>
    /// 判断行列索引是否处于当前表格的有效范围内。
    /// </summary>
    private bool IsCellIndexInRange(int rowIndex, int columnIndex)
    {
        return rowIndex >= 0 && rowIndex < _workingTable.Rows && columnIndex >= 0 && columnIndex < _workingTable.Cols;
    }

    /// <summary>
    /// 将内部单元格状态转换为电子表格显示文本，保留内容类型和内部元素信息提示。
    /// </summary>
    private static string GetCellDisplayValue(TableCell cell)
    {
        if (cell.InnerElements.Count > 0)
        {
            return $"[内部元素 {cell.InnerElements.Count}]";
        }

        var prefix = cell.ContentType switch
        {
            TableCellContentType.Text => string.Empty,
            TableCellContentType.Barcode => "[条码] ",
            TableCellContentType.QrCode => "[二维码] ",
            _ => string.Empty,
        };

        return prefix + cell.Content;
    }

    /// <summary>
    /// 将零基列索引转换为类似 Excel 的字母列名，提升电子表格编辑体验。
    /// </summary>
    private static string GetSpreadsheetColumnName(int columnIndex)
    {
        var value = columnIndex + 1;
        var result = string.Empty;

        while (value > 0)
        {
            value--;
            result = (char)('A' + (value % 26)) + result;
            value /= 26;
        }

        return result;
    }

    /// <summary>
    /// 复制表格元素及其所有单元格数据，确保编辑工作副本与主编辑器状态隔离。
    /// </summary>
    private static TableElement CloneTable(TableElement source)
    {
        return new TableElement
        {
            Id = source.Id,
            X = source.X,
            Y = source.Y,
            Rotation = source.Rotation,
            Rows = source.Rows,
            Cols = source.Cols,
            RowHeight = source.RowHeight,
            RowHeights = source.RowHeights.ToList(),
            ColumnWidths = source.ColumnWidths.ToList(),
            Cells = source.Cells.Select(CloneCell).ToList(),
            BorderStyle = source.BorderStyle,
            GridStyle = source.GridStyle,
        };
    }

    /// <summary>
    /// 复制单元格数据及其内部元素，避免不同编辑器实例共享同一引用对象。
    /// </summary>
    private static TableCell CloneCell(TableCell cell)
    {
        return new TableCell
        {
            ContentType = cell.ContentType,
            Content = cell.Content,
            BarcodeType = cell.BarcodeType,
            QrCellWidth = cell.QrCellWidth,
            QrMode = cell.QrMode,
            QrErrorCorrectionLevel = cell.QrErrorCorrectionLevel,
            InnerElements = cell.InnerElements.Select(CloneInnerElement).ToList(),
        };
    }

    /// <summary>
    /// 复制单元格内部元素，保证复杂内容在工作副本中的编辑不会泄漏回主模板。
    /// </summary>
    private static TableCellInnerElement CloneInnerElement(TableCellInnerElement element)
    {
        return element switch
        {
            TableCellTextElement textElement => new TableCellTextElement
            {
                Id = textElement.Id,
                X = textElement.X,
                Y = textElement.Y,
                Width = textElement.Width,
                Height = textElement.Height,
                Rotation = textElement.Rotation,
                Content = textElement.Content,
                Font = textElement.Font,
                XScale = textElement.XScale,
                YScale = textElement.YScale,
            },
            TableCellBarcodeElement barcodeElement => new TableCellBarcodeElement
            {
                Id = barcodeElement.Id,
                X = barcodeElement.X,
                Y = barcodeElement.Y,
                Width = barcodeElement.Width,
                Height = barcodeElement.Height,
                Rotation = barcodeElement.Rotation,
                Content = barcodeElement.Content,
                BarcodeType = barcodeElement.BarcodeType,
                Narrow = barcodeElement.Narrow,
                Wide = barcodeElement.Wide,
                Readable = barcodeElement.Readable,
            },
            TableCellQrCodeElement qrCodeElement => new TableCellQrCodeElement
            {
                Id = qrCodeElement.Id,
                X = qrCodeElement.X,
                Y = qrCodeElement.Y,
                Width = qrCodeElement.Width,
                Height = qrCodeElement.Height,
                Rotation = qrCodeElement.Rotation,
                Content = qrCodeElement.Content,
                ErrorCorrectionLevel = qrCodeElement.ErrorCorrectionLevel,
                CellWidth = qrCodeElement.CellWidth,
                Mode = qrCodeElement.Mode,
            },
            _ => throw new InvalidOperationException("Unsupported cell inner element type."),
        };
    }
}
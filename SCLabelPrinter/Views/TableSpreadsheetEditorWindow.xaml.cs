using System.Data;
using System.Windows;
using System.Windows.Controls;
using SCLabelPrinter.ViewModels;

namespace SCLabelPrinter.Views;

/// <summary>
/// TableSpreadsheetEditorWindow 的交互逻辑。
/// </summary>
public partial class TableSpreadsheetEditorWindow : Window
{
    public TableSpreadsheetEditorWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// 在取消时关闭窗口并丢弃工作副本。
    /// </summary>
    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    /// <summary>
    /// 在确认前应用当前单元格编辑并关闭窗口。
    /// </summary>
    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is TableSpreadsheetEditorViewModel vm && vm.ApplySelectedCellChangesCommand.CanExecute(null))
        {
            vm.ApplySelectedCellChangesCommand.Execute(null);
        }

        DialogResult = true;
        Close();
    }

    /// <summary>
    /// 为 DataGrid 行设置数字行头，形成更接近电子表格的视觉结构。
    /// </summary>
    private void SpreadsheetGrid_LoadingRow(object sender, DataGridRowEventArgs e)
    {
        e.Row.Header = (e.Row.GetIndex() + 1).ToString();
    }

    /// <summary>
    /// 当选中单元格变化时，将当前焦点同步给编辑器视图模型。
    /// </summary>
    private void SpreadsheetGrid_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
    {
        if (DataContext is not TableSpreadsheetEditorViewModel vm)
        {
            return;
        }

        var currentCell = SpreadsheetGrid.CurrentCell;
        if (currentCell.Column is null || currentCell.Item is not DataRowView)
        {
            return;
        }

        var rowIndex = SpreadsheetGrid.Items.IndexOf(currentCell.Item);
        var columnIndex = currentCell.Column.DisplayIndex;
        vm.SelectCell(rowIndex, columnIndex);
    }

    /// <summary>
    /// 阻止包含内部元素的单元格被直接覆盖，保留其复杂内容的专用编辑入口。
    /// </summary>
    private void SpreadsheetGrid_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
    {
        if (DataContext is not TableSpreadsheetEditorViewModel vm)
        {
            return;
        }

        var rowIndex = e.Row.GetIndex();
        var columnIndex = e.Column.DisplayIndex;
        if (!vm.CanEditGridCell(rowIndex, columnIndex))
        {
            e.Cancel = true;
        }
    }

    /// <summary>
    /// 在 DataGrid 单元格提交时，将文本编辑结果写回工作副本。
    /// </summary>
    private void SpreadsheetGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
    {
        if (e.EditAction != DataGridEditAction.Commit || DataContext is not TableSpreadsheetEditorViewModel vm)
        {
            return;
        }

        if (e.EditingElement is not TextBox textBox)
        {
            return;
        }

        vm.UpdateGridCellText(e.Row.GetIndex(), e.Column.DisplayIndex, textBox.Text);
    }
}
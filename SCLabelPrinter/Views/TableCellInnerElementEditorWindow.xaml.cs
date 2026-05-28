using System.Windows;
using System.Windows.Input;
using SCLabelPrinter.ViewModels;

namespace SCLabelPrinter.Views;

/// <summary>
/// TableCellInnerElementEditorWindow 的交互逻辑。
/// </summary>
public partial class TableCellInnerElementEditorWindow : Window
{
    public TableCellInnerElementEditorWindow()
    {
        InitializeComponent();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is TableCellInnerElementEditorViewModel vm)
        {
            if (vm.ApplySelectedInnerElementChangesCommand.CanExecute(null))
            {
                vm.ApplySelectedInnerElementChangesCommand.Execute(null);
            }
        }

        DialogResult = true;
        Close();
    }
}

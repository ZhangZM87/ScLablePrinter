using System.Windows;

namespace SCLabelPrinter.Views;

/// <summary>
/// TableCellEditorWindow 的交互逻辑。
/// </summary>
public partial class TableCellEditorWindow : Window
{
    public TableCellEditorWindow()
    {
        InitializeComponent();
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}

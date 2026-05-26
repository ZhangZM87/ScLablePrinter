using System.Windows;
using SCLabelPrinter.ViewModels;

namespace SCLabelPrinter;

/// <summary>
/// 提供主窗口与主视图模型的绑定入口。
/// </summary>
public partial class MainWindow : Window
{
    /// <summary>
    /// 创建主窗口并注入主视图模型。
    /// </summary>
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
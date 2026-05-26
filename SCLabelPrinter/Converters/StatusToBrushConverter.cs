using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using SCLabelPrinter.Core.Printers;

namespace SCLabelPrinter.Converters;

/// <summary>
/// 将打印机状态转换为界面展示所需的颜色。
/// </summary>
public sealed class StatusToBrushConverter : IValueConverter
{
    /// <summary>
    /// 将打印机状态值转换为画刷。
    /// </summary>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is PrinterState state
            ? state switch
            {
                PrinterState.Ready => new SolidColorBrush(Color.FromRgb(0x2E, 0x8B, 0x57)),
                PrinterState.Unknown => new SolidColorBrush(Color.FromRgb(0x7A, 0x7A, 0x7A)),
                _ => new SolidColorBrush(Color.FromRgb(0xC0, 0x44, 0x32)),
            }
            : new SolidColorBrush(Color.FromRgb(0x7A, 0x7A, 0x7A));
    }

    /// <summary>
    /// 不支持反向转换画刷为打印机状态。
    /// </summary>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
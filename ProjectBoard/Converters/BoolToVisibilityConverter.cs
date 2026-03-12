using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ProjectBoard.Converters;

public class BoolToVisibilityConverter : IValueConverter
{
    public bool CollapseWhenFalse { get; set; } = true;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b && b)
            return Visibility.Visible;

        return CollapseWhenFalse ? Visibility.Collapsed : Visibility.Hidden;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Visibility v)
            return v == Visibility.Visible;

        return false;
    }
}
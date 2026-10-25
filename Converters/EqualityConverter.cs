using System;
using System.Globalization;
using System.Windows.Data;

namespace OopsGrocery.Converters;

public sealed class EqualityConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 2 || values[0] == null || values[1] == null ||
            values[0] == System.Windows.DependencyProperty.UnsetValue ||
            values[1] == System.Windows.DependencyProperty.UnsetValue)
        {
            return false;
        }

        return values[0].ToString()!.Equals(values[1].ToString(), StringComparison.OrdinalIgnoreCase);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

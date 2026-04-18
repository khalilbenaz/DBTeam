using System;
using System.Globalization;
using System.Windows.Data;

namespace DBTeam.Modules.Diagram.ViewModels;

public sealed class OffsetConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        double v = value is double d ? d : 0;
        double off = 0;
        if (parameter is string s && double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var p)) off = p;
        return v + off;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
}

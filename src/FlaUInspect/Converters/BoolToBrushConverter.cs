using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media;

namespace FlaUInspect.Converters;

public class BoolToBrushConverter : MarkupExtension, IValueConverter
{
    public Brush TrueValue { get; set; } = Brushes.Transparent;
    public Brush FalseValue { get; set; } = Brushes.Transparent;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return (bool)value ? TrueValue : FalseValue;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value == TrueValue;
    }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        return this;
    }
}
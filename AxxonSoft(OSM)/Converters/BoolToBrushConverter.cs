using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Globalization;

namespace AxxonSoft_OSM_.Converters
{
    public class BoolToBrushConverter : IValueConverter
    {
        public static BoolToBrushConverter Instance { get; } = new BoolToBrushConverter();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool boolValue && parameter is string colors)
            {
                var colorParts = colors.Split(';');
                return boolValue ?
                    new SolidColorBrush(Color.Parse(colorParts[0])) :
                    new SolidColorBrush(Color.Parse(colorParts[1]));
            }
            return Brushes.Transparent;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
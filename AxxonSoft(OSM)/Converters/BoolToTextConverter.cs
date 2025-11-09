using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace AxxonSoft_OSM_.Converters
{
    public class BoolToTextConverter : IValueConverter
    {
        public static BoolToTextConverter Instance { get; } = new BoolToTextConverter();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool boolValue && parameter is string texts)
            {
                var textParts = texts.Split(';');
                return boolValue ? textParts[0] : textParts[1];
            }
            return string.Empty;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
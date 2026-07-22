using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace SubRenamer.Converters
{
    public class BoolToHighlightBrushConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool isHighlight)
            {
                return isHighlight
                    ? new SolidColorBrush(Color.FromArgb(80, 0, 120, 215))
                    : new SolidColorBrush(Color.FromArgb(20, 128, 128, 128));
            }
            return Brushes.Transparent;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

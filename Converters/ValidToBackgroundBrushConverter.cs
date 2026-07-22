using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace SubRenamer.Converters
{
    public class ValidToBackgroundBrushConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool isValid)
            {
                return isValid
                    ? Brushes.Transparent
                    : new SolidColorBrush(Color.FromRgb(255, 200, 200));
            }
            return Brushes.Transparent;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

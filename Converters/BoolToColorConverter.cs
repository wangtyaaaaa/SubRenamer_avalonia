using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace SubRenamer.Converters
{
    public class BoolToColorConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool isOther)
            {
                return isOther ? Brushes.Gray : Brushes.Black;
            }
            return Brushes.Black;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

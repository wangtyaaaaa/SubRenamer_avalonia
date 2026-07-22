using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace SubRenamer.Converters
{
    /// <summary>
    /// 布尔值到颜色转换器
    /// 将 bool 值转换为不同的前景色，用于区分"其他字幕文件"组
    /// </summary>
    public class BoolToColorConverter : IValueConverter
    {
        /// <summary>
        /// 将布尔值转换为颜色
        /// </summary>
        /// <param name="value">布尔值</param>
        /// <param name="targetType">目标类型</param>
        /// <param name="parameter">转换参数</param>
        /// <param name="culture">文化信息</param>
        /// <returns>颜色画刷（Gray 或 Black）</returns>
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool isOther)
            {
                return isOther ? Brushes.Gray : Brushes.Black;
            }
            return Brushes.Black;
        }

        /// <summary>
        /// 将颜色转换回布尔值（未实现）
        /// </summary>
        /// <param name="value">颜色画刷</param>
        /// <param name="targetType">目标类型</param>
        /// <param name="parameter">转换参数</param>
        /// <param name="culture">文化信息</param>
        /// <returns>抛出异常</returns>
        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

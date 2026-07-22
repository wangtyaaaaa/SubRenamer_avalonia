using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace SubRenamer.Converters
{
    /// <summary>
    /// 布尔值到透明度转换器
    /// 将 bool 值转换为透明度值（1.0 或 0.0）
    /// </summary>
    public class BoolToOpacityConverter : IValueConverter
    {
        /// <summary>
        /// 将布尔值转换为透明度
        /// </summary>
        /// <param name="value">布尔值</param>
        /// <param name="targetType">目标类型</param>
        /// <param name="parameter">转换参数</param>
        /// <param name="culture">文化信息</param>
        /// <returns>透明度值（1.0 或 0.0）</returns>
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool isTrue)
            {
                return isTrue ? 1.0 : 0.0;
            }
            return 0.0;
        }

        /// <summary>
        /// 将透明度转换回布尔值（未实现）
        /// </summary>
        /// <param name="value">透明度值</param>
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

using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace SubRenamer.Converters
{
    /// <summary>
    /// 布尔值到高亮画刷转换器
    /// 将 bool 值转换为不同的背景色，用于拖拽时高亮显示字幕项
    /// </summary>
    public class BoolToHighlightBrushConverter : IValueConverter
    {
        /// <summary>
        /// 将布尔值转换为背景画刷
        /// </summary>
        /// <param name="value">布尔值</param>
        /// <param name="targetType">目标类型</param>
        /// <param name="parameter">转换参数</param>
        /// <param name="culture">文化信息</param>
        /// <returns>背景画刷（高亮蓝色或灰色）</returns>
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

        /// <summary>
        /// 将背景画刷转换回布尔值（未实现）
        /// </summary>
        /// <param name="value">背景画刷</param>
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

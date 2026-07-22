using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SubRenamer.ViewModels
{
    /// <summary>
    /// 视图模型基类，实现 INotifyPropertyChanged 接口
    /// 提供属性变更通知机制，所有 ViewModel 应继承此类
    /// </summary>
    public class ViewModelBase : INotifyPropertyChanged
    {
        /// <summary>
        /// 属性变更事件，当属性值改变时触发
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// 触发属性变更事件
        /// </summary>
        /// <param name="propertyName">属性名称，默认使用调用方属性名</param>
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// 设置属性值并自动触发变更通知
        /// </summary>
        /// <typeparam name="T">属性类型</typeparam>
        /// <param name="field">字段引用</param>
        /// <param name="value">新值</param>
        /// <param name="propertyName">属性名称，默认使用调用方属性名</param>
        /// <returns>如果值发生变化返回 true，否则返回 false</returns>
        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}

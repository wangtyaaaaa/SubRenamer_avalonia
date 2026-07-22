using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using SubRenamer.ViewModels;
using SubRenamer.Views;

namespace SubRenamer;

/// <summary>
/// 应用程序入口类
/// </summary>
public partial class App : Application
{
    /// <summary>
    /// 初始化应用程序
    /// </summary>
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    /// <summary>
    /// 框架初始化完成后执行
    /// 创建主窗口并设置数据上下文
    /// </summary>
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainViewModel()
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}

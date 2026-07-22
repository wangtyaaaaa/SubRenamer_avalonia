using Avalonia;

namespace SubRenamer;

/// <summary>
/// 程序入口类
/// </summary>
class Program
{
    /// <summary>
    /// 程序主入口点
    /// </summary>
    /// <param name="args">命令行参数</param>
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);

    /// <summary>
    /// 构建 Avalonia 应用程序
    /// </summary>
    /// <returns>Avalonia 应用程序构建器</returns>
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}

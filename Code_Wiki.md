# SubRenamer - Code Wiki (Avalonia 版)

## 1. 项目概述

### 1.1 项目简介

**SubRenamer** 是一个基于 .NET 8.0 和 Avalonia UI 的跨平台桌面应用程序，用于自动将字幕文件与视频文件进行匹配并重命名，使字幕文件名与对应的视频文件名保持一致。

### 1.2 核心功能

| 功能 | 描述 |
|------|------|
| 文件加载 | 支持加载视频文件和字幕文件 |
| 集号解析 | 自动识别文件名中的集号信息 |
| 文件匹配 | 根据集号将字幕文件与视频文件进行匹配 |
| 批量重命名 | 将字幕文件重命名为对应的视频文件名 |
| 撤销操作 | 支持撤销重命名操作 |
| 正则匹配 | 支持自定义正则表达式进行文件匹配 |
| MVVM 架构 | 使用 MVVM 模式，数据绑定驱动 UI |
| 跨平台 | 基于 Avalonia UI，支持 Windows、macOS、Linux |

### 1.3 技术栈

| 类别 | 技术 |
|------|------|
| 框架 | .NET 8.0 |
| UI框架 | Avalonia UI 11.0.7 |
| UI主题 | Fluent Theme |
| 架构模式 | MVVM (Model-View-ViewModel) |
| 语言 | C# 12 |
| 构建工具 | dotnet CLI / MSBuild |
| 项目类型 | Microsoft.NET.Sdk |

---

## 2. 项目架构

### 2.1 整体架构图

```
┌──────────────────────────────────────────────────────────────────────┐
│                         SubRenamer (Avalonia)                        │
├──────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  ┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐  │
│  │     Views       │    │   ViewModels    │    │     Models      │  │
│  │  (视图层)       │───▶│  (视图模型层)    │───▶│  (数据模型层)    │  │
│  │                 │    │                 │    │                 │  │
│  │ - MainWindow    │    │ - MainViewModel │    │ - Names         │  │
│  │   .axaml        │    │ - ViewModelBase │    │ - Renamer       │  │
│  │   .axaml.cs     │    │ - RelayCommand  │    │ - NumberResolver│  │
│  │                 │    │                 │    │ - VSFile/Video  │  │
│  │                 │    │                 │    │   /Sub          │  │
│  │                 │    │                 │    │ - Extentions    │  │
│  └─────────────────┘    └─────────────────┘    └─────────────────┘  │
│                                                                      │
│  ┌─────────────────┐                                                 │
│  │   Converters    │                                                 │
│  │  (值转换器)      │                                                 │
│  │ - BoolToColor   │                                                 │
│  │   Converter     │                                                 │
│  └─────────────────┘                                                 │
│                                                                      │
└──────────────────────────────────────────────────────────────────────┘
```

### 2.2 模块职责划分

| 模块 | 目录 | 职责 |
|------|------|------|
| **Views (视图层)** | `Views/` | 定义 UI 界面，使用 XAML 声明式布局，通过数据绑定与 ViewModel 交互 |
| **ViewModels (视图模型层)** | `ViewModels/` | 封装业务逻辑和 UI 状态，提供命令和属性供视图绑定 |
| **Models (数据模型层)** | `Models/` | 核心业务逻辑、数据结构定义（文件模型、重命名逻辑、集号解析） |
| **Converters (值转换器)** | `Converters/` | XAML 绑定的值转换器 |

### 2.3 项目文件结构

```
SubRenamer/
├── Converters/
│   └── BoolToColorConverter.cs    # 布尔值到颜色的转换器
├── Models/
│   ├── Names.cs                   # 数据模型定义（VSFile、Video、Sub、Names、Extentions）
│   ├── NumberResolver.cs          # 智能集号解析器
│   └── Renamer.cs                 # 重命名核心业务逻辑
├── ViewModels/
│   ├── MainViewModel.cs           # 主窗口 ViewModel
│   └── ViewModelBase.cs           # ViewModel 基类（实现 INotifyPropertyChanged）
├── Views/
│   ├── MainWindow.axaml           # 主窗口 XAML 布局
│   └── MainWindow.axaml.cs        # 主窗口代码后台
├── App.axaml                      # 应用程序资源和样式配置
├── App.axaml.cs                   # 应用程序启动逻辑
├── Program.cs                     # 程序入口点
├── SubRenamer.csproj              # 项目文件
└── SubRenamer.sln                 # 解决方案文件
```

---

## 3. 核心类与函数说明

### 3.1 Program.cs

**功能**: 程序入口点，配置 Avalonia 应用

```csharp
class Program
{
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
```

| 方法 | 说明 |
|------|------|
| `Main(string[] args)` | 应用程序入口 |
| `BuildAvaloniaApp()` | 配置 Avalonia 应用构建器 |

### 3.2 App.axaml / App.axaml.cs

**功能**: 应用程序级配置，包括主题、资源、主窗口初始化

```csharp
public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

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
```

### 3.3 Models 层

#### 3.3.1 Names.cs

**功能**: 定义数据模型和扩展名配置

| 类 | 说明 |
|----|------|
| `Extentions` | 扩展名配置（视频/字幕扩展名列表） |
| `VSFile` | 文件基类（FileInfo、打散的文件名、集号） |
| `Video` | 视频文件类（继承 VSFile） |
| `Sub` | 字幕文件类（继承 VSFile） |
| `Names` | 核心数据模型（文件列表、正则模式配置） |

#### 3.3.2 Renamer.cs

**功能**: 重命名核心业务逻辑

| 方法类别 | 方法 | 说明 |
|----------|------|------|
| **重命名** | `Rename(Names names)` | 根据模式选择重命名策略 |
| | `RenameSubs()` | 将字幕文件重命名为视频文件名 |
| **集号提取** | `GetEpisodeNumber()` | 从文件名提取集号 |
| | `ResolveEpisodeNumber()` | 处理集号字符串（去除 ep/第/集 等） |
| | `IsLikelyEpisodeNumber()` | 判断是否疑似集号 |
| **文件匹配** | `GetSubList()` | 获取匹配指定集号的字幕列表 |
| | `IsFit()` | 判断字幕是否匹配集号 |
| **文件名拆分** | `Split()` | 按分隔符拆分文件名 |
| | `SplitFileNameForGrouping()` | 细粒度拆分文件名 |
| **撤销功能** | `Redo()` | 撤销重命名 |
| | `ClearRedoDic()` | 清空撤销记录 |
| | `IsRedoAvailabel()` | 检查是否可撤销 |
| **事件** | `ProgressChanged` | 进度变更事件 |

#### 3.3.3 NumberResolver.cs

**功能**: 智能集号解析，支持分组匹配和相似度计算

| 方法 | 说明 |
|------|------|
| `Resolve(Names names)` | 逐字符比较法解析集号 |
| `ResolveFileList<T>()` | 使用离散度最高列作为集号 |
| `ResolveGroupFileList<T>()` | 分组解析集号 |
| `GroupVSFiles<T>()` | 按文件名格式分组 |
| `CalculateWeightedSimilarity()` | 加权相似度计算 |
| `FindTopTwoLCS_Optimized()` | 找两个最长无重叠公共子串 |

### 3.4 ViewModels 层

#### 3.4.1 ViewModelBase.cs

**功能**: ViewModel 基类，实现 INotifyPropertyChanged

```csharp
public class ViewModelBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null);
    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null);
}
```

#### 3.4.2 MainViewModel.cs

**功能**: 主窗口视图模型，封装所有 UI 逻辑和状态

| 属性 | 类型 | 说明 |
|------|------|------|
| `FolderPath` | `string` | 文件夹路径 |
| `VideoExts` | `string` | 视频扩展名列表 |
| `SubtitleExts` | `string` | 字幕扩展名列表 |
| `MinMatchRate` | `string` | 最小匹配度阈值 |
| `Delimiter` | `string` | 分隔符 |
| `IsRegexMode` | `bool` | 是否正则模式 |
| `VideoLeft/Right` | `string` | 正则模式视频左右固定部分 |
| `SubtitleLeft/Right` | `string` | 正则模式字幕左右固定部分 |
| `ProgressValue` | `int` | 进度值 |
| `ProgressMax` | `int` | 进度最大值 |
| `StatusMessage` | `string` | 状态消息 |
| `IsBusy` | `bool` | 是否正在处理 |
| `CanUndo` | `bool` | 是否可撤销 |
| `MatchGroups` | `ObservableCollection<FileMatchGroup>` | 匹配组集合 |

| 命令 | 说明 |
|------|------|
| `LoadFilesCommand` | 加载文件 |
| `RenameCommand` | 开始重命名 |
| `UndoCommand` | 撤销重命名 |
| `ResolveCommand` | 解析集号 |
| `BrowseFolderCommand` | 浏览文件夹 |
| `EscapeRegexCommand` | 转义正则字符 |

#### 3.4.3 RelayCommand.cs

**功能**: ICommand 实现，支持同步和异步委托

```csharp
public class RelayCommand : ICommand
{
    public RelayCommand(Func<Task> execute, Func<bool>? canExecute = null)
    public RelayCommand(Action execute, Func<bool>? canExecute = null)
    public bool CanExecute(object? parameter)
    public async void Execute(object? parameter)
    public void RaiseCanExecuteChanged()
}
```

### 3.5 Views 层

#### 3.5.1 MainWindow.axaml

**功能**: 主窗口 XAML 布局

**界面结构**:
- **顶部**: 路径选择（输入框 + 浏览按钮 + 加载按钮）
- **普通模式**: 扩展名、匹配度、分隔符配置，正则模式切换按钮
- **正则模式**: 视频/字幕文件名格式配置（左右固定部分 + 转义字符）
- **操作按钮**: 解析集号、开始重命名、撤销
- **中间区域**: 文件匹配列表（视频 + 对应字幕）
- **底部**: 进度条 + 状态消息

#### 3.5.2 MainWindow.axaml.cs

**功能**: 主窗口代码后台，处理平台相关交互

| 方法 | 说明 |
|------|------|
| `MainWindow()` | 构造函数，初始化 DataContext |
| `ToggleRegexMode_Click()` | 切换正则模式显示 |
| `OnDataContextChanged()` | DataContext 变更时设置 BrowseFolderCommand |
| `BrowseFolderAsync()` | 调用平台文件夹选择对话框 |

### 3.6 Converters 层

#### BoolToColorConverter.cs

**功能**: 布尔值到颜色的转换（用于"其他字幕"组的标题颜色）

```csharp
public class BoolToColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    // true → Gray, false → Black
}
```

---

## 4. 依赖关系

### 4.1 NuGet 包依赖

| 包名 | 版本 | 用途 |
|------|------|------|
| `Avalonia` | 11.0.7 | Avalonia UI 核心库 |
| `Avalonia.Desktop` | 11.0.7 | 桌面平台支持 |
| `Avalonia.Themes.Fluent` | 11.0.7 | Fluent 设计主题 |
| `Avalonia.Fonts.Inter` | 11.0.7 | Inter 字体 |

### 4.2 程序集依赖

| 程序集 | 用途 |
|--------|------|
| `System` | 基础类型 |
| `System.IO` | 文件系统操作 |
| `System.Text.RegularExpressions` | 正则表达式 |
| `System.ComponentModel` | INotifyPropertyChanged |
| `System.Windows.Input` | ICommand |
| `System.Collections.ObjectModel` | ObservableCollection |
| `System.Linq` | LINQ 查询 |

### 4.3 类依赖关系

```
Views (MainWindow)
  ├── uses → MainViewModel (DataContext)
  │     ├── inherits → ViewModelBase
  │     ├── uses → RelayCommand (ICommand 实现)
  │     └── uses → Models (Renamer, NumberResolver, Names)
  └── uses → Converters (BoolToColorConverter)

ViewModels (MainViewModel)
  ├── Renamer (重命名逻辑)
  │     └── Names/VSFile/Video/Sub (数据模型)
  └── NumberResolver (集号解析)
        └── Renamer (辅助方法)

Models
  ├── Names.cs
  │     ├── Extentions (扩展名配置)
  │     ├── VSFile (基类)
  │     ├── Video (继承 VSFile)
  │     ├── Sub (继承 VSFile)
  │     └── Names (文件集合)
  ├── Renamer.cs (重命名逻辑)
  └── NumberResolver.cs (集号解析)
```

---

## 5. 核心算法

（与 WinForms 版本相同，详见完整的算法说明）

### 5.1 集号提取算法
### 5.2 智能集号解析算法
### 5.3 文件分组算法
### 5.4 加权相似度算法

---

## 6. 项目运行方式

### 6.1 开发环境

- **IDE**: Visual Studio 2022 / Rider / VS Code
- **框架**: .NET 8.0 SDK
- **操作系统**: Windows 10+/macOS/Linux（跨平台）

### 6.2 构建命令

```bash
# 构建 Debug 版本
dotnet build

# 构建 Release 版本
dotnet build --configuration Release

# 发布（Windows 单文件）
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true

# 发布（macOS）
dotnet publish -c Release -r osx-x64 --self-contained true

# 发布（Linux）
dotnet publish -c Release -r linux-x64 --self-contained true
```

### 6.3 运行方式

```bash
# 开发模式运行
dotnet run

# 运行已构建的程序
./bin/Debug/net8.0/SubRenamer.exe   # Windows
./bin/Debug/net8.0/SubRenamer       # macOS / Linux
```

### 6.4 配置说明

**扩展名配置**（默认值）:
- 视频扩展名: `mp4,mkv`
- 字幕扩展名: `ass,ssa,sub,srt`

可通过界面文本框实时修改。

---

## 7. MVVM 数据绑定说明

### 7.1 数据流向

```
用户操作 → View (XAML) → Command → ViewModel → Model
                                                       │
                                                       ▼
属性变更 ← PropertyChanged ← ViewModel 更新 ← 业务逻辑执行
```

### 7.2 主要绑定

| UI 元素 | 绑定属性 | 源属性 |
|---------|----------|--------|
| 路径文本框 | Text | FolderPath |
| 浏览按钮 | Command | BrowseFolderCommand |
| 加载按钮 | Command | LoadFilesCommand |
| 普通模式面板 | IsVisible | !IsRegexMode |
| 正则模式面板 | IsVisible | IsRegexMode |
| 开始重命名按钮 | Command | RenameCommand |
| 撤销按钮 | Command, IsEnabled | UndoCommand, CanUndo |
| 文件列表 | ItemsSource | MatchGroups |
| 进度条 | Value, Maximum, IsIndeterminate | ProgressValue, ProgressMax, IsBusy |
| 状态文本 | Text | StatusMessage |

---

## 8. 编译配置

| 配置 | 说明 |
|------|------|
| Debug | 调试模式，包含调试符号 |
| Release | 发布模式，优化代码 |

项目文件关键属性：
- `TargetFramework`: `net8.0`
- `Nullable`: `enable`
- `ImplicitUsings`: `enable`
- `AvaloniaUseCompiledBindingsByDefault`: `true`
- `GenerateAssemblyInfo`: `false`

---

## 9. 代码规范与约定

### 9.1 命名约定

| 类型 | 约定 | 示例 |
|------|------|------|
| 类/接口 | PascalCase | `MainViewModel`, `ICommand` |
| 方法 | PascalCase | `LoadFilesAsync`, `RenameSubs` |
| 属性 | PascalCase | `FolderPath`, `IsBusy` |
| 私有字段 | camelCase + 下划线前缀 | `_names`, `_execute` |
| 局部变量 | camelCase | `videoDic`, `matchRate` |
| 接口 | I + PascalCase | `INotifyPropertyChanged` |

### 9.2 XAML 规范

- 使用 MVVM 数据绑定，避免在 code-behind 写业务逻辑
- 布局优先使用 Grid、StackPanel
- 颜色和样式优先使用系统主题资源
- 编译绑定（CompiledBindings）默认启用

### 9.3 设计模式

- **MVVM 模式**: Model-View-ViewModel 分离
- **命令模式**: RelayCommand 封装操作
- **观察者模式**: INotifyPropertyChanged 通知属性变更
- **分层架构**: Models / ViewModels / Views 三层分离

---

## 10. 迁移说明（WinForms → Avalonia）

### 10.1 主要变更

| 原 WinForms | 新 Avalonia | 说明 |
|-------------|-------------|------|
| `Form1.cs` | `Views/MainWindow.axaml + .axaml.cs` | 主窗口从代码创建改为 XAML 声明式 |
| 事件驱动 | MVVM + 数据绑定 | 从事件驱动改为数据驱动 |
| `BackgroundWorker` | `async/await + Task.Run` | 异步操作从 BackgroundWorker 改为 TAP |
| `MessageBox` | （可扩展为对话框） | 消息提示方式 |
| `FolderBrowserDialog` | `StorageProvider.OpenFolderPickerAsync` | 平台抽象的文件对话框 |
| 拖放功能 | （可扩展） | Avalonia 有自己的拖放 API |

### 10.2 保留不变

- 核心业务逻辑（`Renamer.cs`、`NumberResolver.cs`、数据模型）
- 算法实现完全一致
- 文件操作逻辑相同

---

## 11. 扩展与维护

### 11.1 添加新功能步骤

1. **Model 层**: 如需新数据或业务逻辑，在 `Models/` 添加或修改
2. **ViewModel 层**: 在 `MainViewModel` 添加属性和命令
3. **View 层**: 在 XAML 中添加 UI 元素并绑定
4. **Converter**（可选）: 如需值转换，在 `Converters/` 添加

### 11.2 添加新的文件扩展名

修改 `Models/Names.cs` 中 `Extentions` 类的默认值，或通过界面配置。

### 11.3 添加新窗口

1. 在 `Views/` 创建 `xxxWindow.axaml` 和 `xxxWindow.axaml.cs`
2. 在 `ViewModels/` 创建对应的 `xxxViewModel.cs`
3. 在需要打开的地方实例化并 Show/ShowDialog

---

## 12. 总结

Avalonia 版本的 SubRenamer 在保留原有核心功能的基础上，带来以下改进：

1. **跨平台**: 支持 Windows、macOS、Linux
2. **现代化架构**: MVVM 模式，代码更清晰，测试更容易
3. **声明式 UI**: XAML 布局，界面与逻辑分离
4. **可扩展**: 分层架构便于添加新功能
5. **现代化技术栈**: .NET 8 + Avalonia 11

核心业务逻辑（集号解析、文件匹配、重命名算法）完全保留，仅 UI 层和架构模式发生变化。

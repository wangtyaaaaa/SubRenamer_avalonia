using System.Collections.ObjectModel;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using SubRenamer.Models;

namespace SubRenamer.ViewModels
{
    /// <summary>
    /// 文件匹配组，包含一个视频文件和对应的字幕文件列表
    /// </summary>
    public class FileMatchGroup : ViewModelBase
    {
        private string _videoName = string.Empty;
        /// <summary>
        /// 视频文件名
        /// </summary>
        public string VideoName
        {
            get => _videoName;
            set => SetProperty(ref _videoName, value);
        }

        private FileInfo? _videoFile;
        /// <summary>
        /// 视频文件信息
        /// </summary>
        public FileInfo? VideoFile
        {
            get => _videoFile;
            set => SetProperty(ref _videoFile, value);
        }

        /// <summary>
        /// 字幕文件列表
        /// </summary>
        public ObservableCollection<SubtitleItem> Subtitles { get; set; } = new();

        private bool _isOtherGroup;
        /// <summary>
        /// 是否为"其他字幕文件"组
        /// </summary>
        public bool IsOtherGroup
        {
            get => _isOtherGroup;
            set => SetProperty(ref _isOtherGroup, value);
        }

        private bool _isDragOver;
        /// <summary>
        /// 是否有字幕正在拖拽到此组上方
        /// </summary>
        public bool IsDragOver
        {
            get => _isDragOver;
            set
            {
                if (SetProperty(ref _isDragOver, value))
                {
                    OnPropertyChanged(nameof(BackgroundBrush));
                }
            }
        }

        /// <summary>
        /// 背景画刷，根据拖拽状态动态变化
        /// </summary>
        public Brush BackgroundBrush => _isDragOver
            ? new SolidColorBrush(Color.FromArgb(80, 0, 120, 215))
            : new SolidColorBrush(Color.FromArgb(20, 128, 128, 128));
    }

    /// <summary>
    /// 字幕项，代表一个字幕文件
    /// </summary>
    public class SubtitleItem : ViewModelBase
    {
        private string _name = string.Empty;
        /// <summary>
        /// 字幕文件名
        /// </summary>
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        private FileInfo? _file;
        /// <summary>
        /// 字幕文件信息
        /// </summary>
        public FileInfo? File
        {
            get => _file;
            set => SetProperty(ref _file, value);
        }

        private bool _isDragging;
        /// <summary>
        /// 是否正在被拖拽
        /// </summary>
        public bool IsDragging
        {
            get => _isDragging;
            set => SetProperty(ref _isDragging, value);
        }

        /// <summary>
        /// 所属的文件匹配组（用于拖拽时快速查找源组）
        /// </summary>
        public FileMatchGroup? ParentGroup { get; set; }
    }

    /// <summary>
    /// 主窗口视图模型，包含所有业务逻辑和数据管理
    /// </summary>
    public class MainViewModel : ViewModelBase
    {
        private string _folderPath = Environment.CurrentDirectory;
        /// <summary>
        /// 当前选择的文件夹路径
        /// </summary>
        public string FolderPath
        {
            get => _folderPath;
            set => SetProperty(ref _folderPath, value);
        }

        private string _videoExts = "mp4,mkv";
        /// <summary>
        /// 视频文件扩展名列表（逗号分隔）
        /// </summary>
        public string VideoExts
        {
            get => _videoExts;
            set
            {
                if (SetProperty(ref _videoExts, value))
                {
                    Extentions.SetExts(value, Extentions.VIDEO);
                }
            }
        }

        private string _subtitleExts = "ass,ssa,sub,srt";
        /// <summary>
        /// 字幕文件扩展名列表（逗号分隔）
        /// </summary>
        public string SubtitleExts
        {
            get => _subtitleExts;
            set
            {
                if (SetProperty(ref _subtitleExts, value))
                {
                    Extentions.SetExts(value, Extentions.SUB);
                }
            }
        }

        private string _minMatchRate = "0.7";
        /// <summary>
        /// 文件名匹配度阈值（0-1之间）
        /// </summary>
        public string MinMatchRate
        {
            get => _minMatchRate;
            set
            {
                if (SetProperty(ref _minMatchRate, value))
                {
                    OnPropertyChanged(nameof(IsMatchRateValid));
                }
            }
        }

        /// <summary>
        /// 匹配度阈值是否有效（0到1之间的小数）
        /// </summary>
        public bool IsMatchRateValid
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_minMatchRate))
                    return false;
                if (double.TryParse(_minMatchRate, out double rate))
                {
                    return rate >= 0 && rate <= 1;
                }
                return false;
            }
        }

        private string? _delimiter = null;
        /// <summary>
        /// 分隔符，用于提取字幕扩展名
        /// </summary>
        public string? Delimiter
        {
            get => _delimiter;
            set => SetProperty(ref _delimiter, value);
        }

        private bool _isRegexMode;
        /// <summary>
        /// 是否启用正则模式
        /// </summary>
        public bool IsRegexMode
        {
            get => _isRegexMode;
            set => SetProperty(ref _isRegexMode, value);
        }

        private string _videoLeft = string.Empty;
        /// <summary>
        /// 正则模式下视频文件名左边固定部分
        /// </summary>
        public string VideoLeft
        {
            get => _videoLeft;
            set => SetProperty(ref _videoLeft, value);
        }

        private string _videoRight = string.Empty;
        /// <summary>
        /// 正则模式下视频文件名右边固定部分
        /// </summary>
        public string VideoRight
        {
            get => _videoRight;
            set => SetProperty(ref _videoRight, value);
        }

        private string _subtitleLeft = string.Empty;
        /// <summary>
        /// 正则模式下字幕文件名左边固定部分
        /// </summary>
        public string SubtitleLeft
        {
            get => _subtitleLeft;
            set => SetProperty(ref _subtitleLeft, value);
        }

        private string _subtitleRight = string.Empty;
        /// <summary>
        /// 正则模式下字幕文件名右边固定部分
        /// </summary>
        public string SubtitleRight
        {
            get => _subtitleRight;
            set => SetProperty(ref _subtitleRight, value);
        }

        private int _progressValue;
        /// <summary>
        /// 进度条当前值
        /// </summary>
        public int ProgressValue
        {
            get => _progressValue;
            set => SetProperty(ref _progressValue, value);
        }

        private int _progressMax = 100;
        /// <summary>
        /// 进度条最大值
        /// </summary>
        public int ProgressMax
        {
            get => _progressMax;
            set => SetProperty(ref _progressMax, value);
        }

        private string _statusMessage = "就绪";
        /// <summary>
        /// 状态栏消息
        /// </summary>
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        private bool _isBusy;
        /// <summary>
        /// 是否正在执行耗时操作
        /// </summary>
        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }

        private bool _canUndo;
        /// <summary>
        /// 是否可以撤销操作
        /// </summary>
        public bool CanUndo
        {
            get => _canUndo;
            set => SetProperty(ref _canUndo, value);
        }

        /// <summary>
        /// 文件匹配组列表
        /// </summary>
        public ObservableCollection<FileMatchGroup> MatchGroups { get; set; } = new();

        /// <summary>
        /// 文件名称解析器实例
        /// </summary>
        private Names? _names;

        /// <summary>
        /// 加载文件命令
        /// </summary>
        public ICommand LoadFilesCommand { get; }
        /// <summary>
        /// 重命名命令
        /// </summary>
        public RelayCommand RenameCommand { get; }
        /// <summary>
        /// 撤销命令
        /// </summary>
        public RelayCommand UndoCommand { get; }
        /// <summary>
        /// 解析集号命令
        /// </summary>
        public ICommand ResolveCommand { get; }
        /// <summary>
        /// 浏览文件夹命令
        /// </summary>
        public ICommand BrowseFolderCommand { get; set; }
        /// <summary>
        /// 转义正则表达式命令
        /// </summary>
        public ICommand EscapeRegexCommand { get; }

        /// <summary>
        /// 构造函数，初始化所有命令
        /// </summary>
        public MainViewModel()
        {
            LoadFilesCommand = new RelayCommand(async () => await LoadFilesAsync(), () => !IsBusy);
            RenameCommand = new RelayCommand(async () => await RenameAsync(), () => !IsBusy && MatchGroups.Any(g => g.Subtitles.Any()));
            UndoCommand = new RelayCommand(async () => await UndoAsync(), () => !IsBusy && CanUndo);
            ResolveCommand = new RelayCommand(async () => await ResolveAsync(), () => !IsBusy);
            BrowseFolderCommand = new RelayCommand(async () => await BrowseFolderAsync(), () => !IsBusy);
            EscapeRegexCommand = new RelayCommand(() => EscapeRegex(), () => !IsBusy);
        }

        /// <summary>
        /// 浏览文件夹，打开系统文件夹选择对话框
        /// </summary>
        private async Task BrowseFolderAsync()
        {
            var app = Avalonia.Application.Current;
            if (app?.ApplicationLifetime is not Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop) return;

            var window = desktop.MainWindow;
            // 2. 判断窗口和 StorageProvider 是否存在
            if (window?.StorageProvider == null) return;

            // 3. 使用新的 StorageProvider API 打开文件夹选择器
            var folders = await window.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "选择文件夹",
                AllowMultiple = false // 不允许选择多个文件夹
            });

            // 4. 判断用户是否选择了文件夹并获取路径
            if (folders.Count > 0)
            {
                FolderPath = folders[0].TryGetLocalPath() ?? folders[0].Name;
            }
        }

        /// <summary>
        /// 设置文件夹路径
        /// </summary>
        /// <param name="path">文件夹路径</param>
        public void SetFolderPath(string path)
        {
            FolderPath = path;
        }

        /// <summary>
        /// 异步加载文件列表
        /// </summary>
        private async Task LoadFilesAsync()
        {
            if (string.IsNullOrWhiteSpace(FolderPath) || !Directory.Exists(FolderPath))
            {
                StatusMessage = "请选择有效的文件夹路径";
                return;
            }

            IsBusy = true;
            StatusMessage = "正在加载文件...";
            MatchGroups.Clear();

            try
            {
                await Task.Run(() =>
                {
                    var dInfo = new DirectoryInfo(FolderPath);

                    if (IsRegexMode)
                    {
                        _names = new Names(dInfo, VideoLeft, VideoRight, SubtitleLeft, SubtitleRight);
                        LoadRegexMode();
                    }
                    else
                    {
                        _names = new Names(dInfo);
                        LoadNormalMode();
                    }
                });

                StatusMessage = $"加载完成，共 {MatchGroups.Count} 组";
            }
            catch (Exception ex)
            {
                StatusMessage = $"加载失败: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
                UpdateCanUndo();
                RenameCommand.RaiseCanExecuteChanged();
            }
        }

        /// <summary>
        /// 加载普通模式（自动匹配）
        /// </summary>
        private void LoadNormalMode()
        {
            if (_names == null) return;

            NumberResolver.ResolveFileList(_names.videos);

            double.TryParse(MinMatchRate, out double rate);
            NumberResolver.ResolveGroupFileList(_names.subs, rate);

            var allSubs = VSFile.FileListTOFileInfoList(_names.subs);

            foreach (var video in _names.videos)
            {
                var group = new FileMatchGroup
                {
                    VideoName = video.File.Name,
                    VideoFile = video.File
                };

                if (!string.IsNullOrEmpty(video.Num))
                {
                    var matchedSubs = Renamer.GetSubListByNum(_names, video.Num);
                    if (matchedSubs.Count == 0)
                    {
                        matchedSubs = Renamer.GetSubList(_names, video.Num);
                    }
                    foreach (var sub in matchedSubs)
                    {
                        group.Subtitles.Add(new SubtitleItem { Name = sub.Name, File = sub, ParentGroup = group });
                        allSubs.Remove(sub);
                    }
                }

                MatchGroups.Add(group);
            }

            if (allSubs.Count > 0)
            {
                var otherGroup = new FileMatchGroup
                {
                    VideoName = "其他字幕文件",
                    IsOtherGroup = true
                };
                foreach (var sub in allSubs)
                {
                    otherGroup.Subtitles.Add(new SubtitleItem { Name = sub.Name, File = sub, ParentGroup = otherGroup });
                }
                MatchGroups.Add(otherGroup);
            }
        }

        /// <summary>
        /// 加载正则模式（用户自定义匹配规则）
        /// </summary>
        private void LoadRegexMode()
        {
            if (_names == null) return;

            var allSubs = VSFile.FileListTOFileInfoList(_names.subs);

            var videoDic = Renamer.GetDic(VSFile.FileListTOFileInfoList(_names.videos), _names.GetVideoReplasePattern());
            var subDic = Renamer.GetDic(VSFile.FileListTOFileInfoList(_names.subs), _names.GetSubReplasePattern());

            foreach (var video in videoDic.Keys)
            {
                var group = new FileMatchGroup
                {
                    VideoName = video.Name,
                    VideoFile = video
                };

                var subs = Renamer.GetSubList(subDic, videoDic[video]);
                foreach (var sub in subs)
                {
                    group.Subtitles.Add(new SubtitleItem { Name = sub.Name, File = sub, ParentGroup = group });
                    allSubs.Remove(sub);
                }

                MatchGroups.Add(group);
            }

            if (allSubs.Count > 0)
            {
                var otherGroup = new FileMatchGroup
                {
                    VideoName = "其他字幕文件",
                    IsOtherGroup = true
                };
                foreach (var sub in allSubs)
                {
                    otherGroup.Subtitles.Add(new SubtitleItem { Name = sub.Name, File = sub, ParentGroup = otherGroup });
                }
                MatchGroups.Add(otherGroup);
            }
        }

        /// <summary>
        /// 异步执行重命名操作
        /// </summary>
        private async Task RenameAsync()
        {
            if (MatchGroups.Count == 0)
            {
                StatusMessage = "没有可重命名的文件";
                return;
            }

            IsBusy = true;
            StatusMessage = "正在重命名...";
            ProgressValue = 0;
            ProgressMax = MatchGroups.Count(g => g.VideoFile != null);

            try
            {
                await Task.Run(() =>
                {
                    Renamer.ClearRedoDic();
                    int count = 0;

                    foreach (var group in MatchGroups)
                    {
                        if (group.VideoFile == null || group.Subtitles.Count == 0)
                            continue;

                        count++;
                        ProgressValue = count;
                        StatusMessage = group.VideoName;

                        var subs = group.Subtitles.Where(s => s.File != null).Select(s => s.File!).ToList();
                        Renamer.RenameSubs(group.VideoFile, subs, Delimiter);
                    }
                });

                StatusMessage = "重命名完成";
                await LoadFilesAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"重命名失败: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
                UpdateCanUndo();
            }
        }

        /// <summary>
        /// 异步执行撤销操作
        /// </summary>
        private async Task UndoAsync()
        {
            if (!Renamer.IsRedoAvailabel())
            {
                StatusMessage = "没有可撤销的操作";
                return;
            }

            IsBusy = true;
            StatusMessage = "正在撤销...";

            try
            {
                await Task.Run(() =>
                {
                    Renamer.Redo();
                });

                StatusMessage = "撤销成功";
                await LoadFilesAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"撤销失败: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
                UpdateCanUndo();
            }
        }

        /// <summary>
        /// 异步执行集号解析操作
        /// </summary>
        private async Task ResolveAsync()
        {
            if (_names == null)
            {
                StatusMessage = "请先加载文件";
                return;
            }

            IsBusy = true;
            StatusMessage = "正在解析集号...";

            try
            {
                await Task.Run(() =>
                {
                    if (NumberResolver.Resolve(_names))
                    {
                        _names.Resolved = true;
                    }
                });

                if (_names.Resolved)
                {
                    MatchGroups.Clear();
                    LoadResolvedMode();
                    StatusMessage = "集号解析成功";
                }
                else
                {
                    StatusMessage = "集号解析失败";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"解析失败: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// 加载解析模式（使用已解析的集号匹配）
        /// </summary>
        private void LoadResolvedMode()
        {
            if (_names == null) return;

            var allSubs = VSFile.FileListTOFileInfoList(_names.subs);

            foreach (var video in _names.videos)
            {
                if (string.IsNullOrEmpty(video.Num))
                    continue;

                var group = new FileMatchGroup
                {
                    VideoName = video.File.Name,
                    VideoFile = video.File
                };

                var subs = Renamer.GetSubList(_names, video.Num);
                foreach (var sub in subs)
                {
                    group.Subtitles.Add(new SubtitleItem { Name = sub.Name, File = sub, ParentGroup = group });
                    allSubs.Remove(sub);
                }

                MatchGroups.Add(group);
            }

            if (allSubs.Count > 0)
            {
                var otherGroup = new FileMatchGroup
                {
                    VideoName = "其他字幕文件",
                    IsOtherGroup = true
                };
                foreach (var sub in allSubs)
                {
                    otherGroup.Subtitles.Add(new SubtitleItem { Name = sub.Name, File = sub, ParentGroup = otherGroup });
                }
                MatchGroups.Add(otherGroup);
            }
        }

        /// <summary>
        /// 转义正则表达式特殊字符
        /// </summary>
        private void EscapeRegex()
        {
            VideoLeft = EscapeRegexString(VideoLeft);
            VideoRight = EscapeRegexString(VideoRight);
            SubtitleLeft = EscapeRegexString(SubtitleLeft);
            SubtitleRight = EscapeRegexString(SubtitleRight);
        }

        /// <summary>
        /// 转义字符串中的正则表达式特殊字符
        /// </summary>
        /// <param name="input">输入字符串</param>
        /// <returns>转义后的字符串</returns>
        private static string EscapeRegexString(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            return input
                .Replace("\\", "\\\\")
                .Replace("]", "\\]")
                .Replace("[", "\\[")
                .Replace("}", "\\}")
                .Replace("{", "\\{")
                .Replace(")", "\\)")
                .Replace("(", "\\(")
                .Replace("^", "\\^")
                .Replace("$", "\\$")
                .Replace("|", "\\|")
                .Replace("*", "\\*")
                .Replace("+", "\\+")
                .Replace(".", "\\.")
                .Replace("?", "\\?");
        }

        /// <summary>
        /// 移动字幕到目标组
        /// </summary>
        /// <param name="sourceGroup">源组</param>
        /// <param name="targetGroup">目标组</param>
        /// <param name="subtitle">要移动的字幕</param>
        public void MoveSubtitle(FileMatchGroup sourceGroup, FileMatchGroup targetGroup, SubtitleItem subtitle)
        {
            if (!sourceGroup.Subtitles.Contains(subtitle)) return;

            if (sourceGroup != targetGroup)
            {
                sourceGroup.Subtitles.Remove(subtitle);
                targetGroup.Subtitles.Add(subtitle);
                subtitle.ParentGroup = targetGroup;
            }

            ClearDragStates();
            RenameCommand.RaiseCanExecuteChanged();
        }

        /// <summary>
        /// 清除所有拖拽状态
        /// </summary>
        public void ClearDragStates()
        {
            foreach (var group in MatchGroups)
            {
                group.IsDragOver = false;
                foreach (var sub in group.Subtitles)
                {
                    sub.IsDragging = false;
                }
            }
        }

        /// <summary>
        /// 设置当前拖拽悬停的组
        /// </summary>
        /// <param name="group">悬停的组，null表示没有悬停</param>
        public void SetDragOverGroup(FileMatchGroup? group)
        {
            foreach (var g in MatchGroups)
            {
                g.IsDragOver = (g == group);
            }
        }

        /// <summary>
        /// 更新撤销按钮状态
        /// </summary>
        private void UpdateCanUndo()
        {
            CanUndo = Renamer.IsRedoAvailabel();
            UndoCommand.RaiseCanExecuteChanged();
        }
    }

    /// <summary>
    /// 通用命令实现类，支持同步和异步操作
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Func<Task>? _executeAsync;
        private readonly Action? _execute;
        private readonly Func<bool> _canExecute;

        /// <summary>
        /// 构造函数（异步执行）
        /// </summary>
        /// <param name="execute">异步执行方法</param>
        /// <param name="canExecute">是否可执行的判断方法</param>
        public RelayCommand(Func<Task> execute, Func<bool>? canExecute = null)
        {
            _executeAsync = execute;
            _canExecute = canExecute ?? (() => true);
        }

        /// <summary>
        /// 构造函数（同步执行）
        /// </summary>
        /// <param name="execute">同步执行方法</param>
        /// <param name="canExecute">是否可执行的判断方法</param>
        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute ?? (() => true);
        }

        /// <summary>
        /// 可执行状态变更事件
        /// </summary>
        public event EventHandler? CanExecuteChanged;

        /// <summary>
        /// 判断命令是否可执行
        /// </summary>
        /// <param name="parameter">命令参数</param>
        /// <returns>是否可执行</returns>
        public bool CanExecute(object? parameter) => _canExecute();

        /// <summary>
        /// 执行命令
        /// </summary>
        /// <param name="parameter">命令参数</param>
        public async void Execute(object? parameter)
        {
            if (_executeAsync != null)
            {
                await _executeAsync();
            }
            else
            {
                _execute?.Invoke();
            }
        }

        /// <summary>
        /// 触发可执行状态变更事件
        /// </summary>
        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}

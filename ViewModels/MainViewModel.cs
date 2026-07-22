using System.Collections.ObjectModel;
using System.Windows.Input;
using SubRenamer.Models;

namespace SubRenamer.ViewModels
{
    public class FileMatchGroup : ViewModelBase
    {
        private string _videoName = string.Empty;
        public string VideoName
        {
            get => _videoName;
            set => SetProperty(ref _videoName, value);
        }

        private FileInfo? _videoFile;
        public FileInfo? VideoFile
        {
            get => _videoFile;
            set => SetProperty(ref _videoFile, value);
        }

        public ObservableCollection<SubtitleItem> Subtitles { get; set; } = new();

        private bool _isOtherGroup;
        public bool IsOtherGroup
        {
            get => _isOtherGroup;
            set => SetProperty(ref _isOtherGroup, value);
        }
    }

    public class SubtitleItem : ViewModelBase
    {
        private string _name = string.Empty;
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        private FileInfo? _file;
        public FileInfo? File
        {
            get => _file;
            set => SetProperty(ref _file, value);
        }
    }

    public class MainViewModel : ViewModelBase
    {
        private string _folderPath = Environment.CurrentDirectory;
        public string FolderPath
        {
            get => _folderPath;
            set => SetProperty(ref _folderPath, value);
        }

        private string _videoExts = "mp4,mkv";
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

        private string _minMatchRate = "0.5";
        public string MinMatchRate
        {
            get => _minMatchRate;
            set => SetProperty(ref _minMatchRate, value);
        }

        private string? _delimiter = null;
        public string? Delimiter
        {
            get => _delimiter;
            set => SetProperty(ref _delimiter, value);
        }

        private bool _isRegexMode;
        public bool IsRegexMode
        {
            get => _isRegexMode;
            set => SetProperty(ref _isRegexMode, value);
        }

        private string _videoLeft = string.Empty;
        public string VideoLeft
        {
            get => _videoLeft;
            set => SetProperty(ref _videoLeft, value);
        }

        private string _videoRight = string.Empty;
        public string VideoRight
        {
            get => _videoRight;
            set => SetProperty(ref _videoRight, value);
        }

        private string _subtitleLeft = string.Empty;
        public string SubtitleLeft
        {
            get => _subtitleLeft;
            set => SetProperty(ref _subtitleLeft, value);
        }

        private string _subtitleRight = string.Empty;
        public string SubtitleRight
        {
            get => _subtitleRight;
            set => SetProperty(ref _subtitleRight, value);
        }

        private int _progressValue;
        public int ProgressValue
        {
            get => _progressValue;
            set => SetProperty(ref _progressValue, value);
        }

        private int _progressMax = 100;
        public int ProgressMax
        {
            get => _progressMax;
            set => SetProperty(ref _progressMax, value);
        }

        private string _statusMessage = "就绪";
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }

        private bool _canUndo;
        public bool CanUndo
        {
            get => _canUndo;
            set => SetProperty(ref _canUndo, value);
        }

        public ObservableCollection<FileMatchGroup> MatchGroups { get; set; } = new();

        private Names? _names;

        public ICommand LoadFilesCommand { get; }
        public RelayCommand RenameCommand { get; }
        public RelayCommand UndoCommand { get; }
        public ICommand ResolveCommand { get; }
        public ICommand BrowseFolderCommand { get; set; }
        public ICommand EscapeRegexCommand { get; }

        public MainViewModel()
        {
            LoadFilesCommand = new RelayCommand(async () => await LoadFilesAsync(), () => !IsBusy);
            RenameCommand = new RelayCommand(async () => await RenameAsync(), () => !IsBusy && MatchGroups.Any(g => g.Subtitles.Any()));
            UndoCommand = new RelayCommand(async () => await UndoAsync(), () => !IsBusy && CanUndo);
            ResolveCommand = new RelayCommand(async () => await ResolveAsync(), () => !IsBusy);
            BrowseFolderCommand = new RelayCommand(async () => await BrowseFolderAsync(), () => !IsBusy);
            EscapeRegexCommand = new RelayCommand(() => EscapeRegex(), () => !IsBusy);
        }

        private async Task BrowseFolderAsync()
        {
            await Task.Run(() => { });
        }

        public void SetFolderPath(string path)
        {
            FolderPath = path;
        }

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
                    // 优先使用已解析的 Num 属性匹配
                    var matchedSubs = Renamer.GetSubListByNum(_names, video.Num);
                    if (matchedSubs.Count == 0)
                    {
                        // 如果没有匹配到，尝试用集号匹配文件名
                        matchedSubs = Renamer.GetSubList(_names, video.Num);
                    }
                    foreach (var sub in matchedSubs)
                    {
                        group.Subtitles.Add(new SubtitleItem { Name = sub.Name, File = sub });
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
                    otherGroup.Subtitles.Add(new SubtitleItem { Name = sub.Name, File = sub });
                }
                MatchGroups.Add(otherGroup);
            }
        }

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
                    group.Subtitles.Add(new SubtitleItem { Name = sub.Name, File = sub });
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
                    otherGroup.Subtitles.Add(new SubtitleItem { Name = sub.Name, File = sub });
                }
                MatchGroups.Add(otherGroup);
            }
        }

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
                    group.Subtitles.Add(new SubtitleItem { Name = sub.Name, File = sub });
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
                    otherGroup.Subtitles.Add(new SubtitleItem { Name = sub.Name, File = sub });
                }
                MatchGroups.Add(otherGroup);
            }
        }

        private void EscapeRegex()
        {
            VideoLeft = EscapeRegexString(VideoLeft);
            VideoRight = EscapeRegexString(VideoRight);
            SubtitleLeft = EscapeRegexString(SubtitleLeft);
            SubtitleRight = EscapeRegexString(SubtitleRight);
        }

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

        private void UpdateCanUndo()
        {
            CanUndo = Renamer.IsRedoAvailabel();
            UndoCommand.RaiseCanExecuteChanged();
        }
    }

    public class RelayCommand : ICommand
    {
        private readonly Func<Task>? _executeAsync;
        private readonly Action? _execute;
        private readonly Func<bool> _canExecute;

        public RelayCommand(Func<Task> execute, Func<bool>? canExecute = null)
        {
            _executeAsync = execute;
            _canExecute = canExecute ?? (() => true);
        }

        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute ?? (() => true);
        }

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter) => _canExecute();

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

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}

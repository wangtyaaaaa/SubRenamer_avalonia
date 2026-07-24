using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;
using SubRenamer.ViewModels;

namespace SubRenamer.Views
{
    /// <summary>
    /// 主窗口代码后台，处理UI交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// 当前正在拖拽的字幕项
        /// </summary>
        private SubtitleItem? _draggingSubtitle;
        /// <summary>
        /// 当前拖拽字幕所属的源组
        /// </summary>
        private FileMatchGroup? _draggingSourceGroup;
        /// <summary>
        /// 拖拽时显示的幽灵控件（跟随鼠标的预览）
        /// </summary>
        private Border? _draggingGhost;
        /// <summary>
        /// 承载幽灵控件的画布
        /// </summary>
        private Canvas? _dragGhostCanvas;
        /// <summary>
        /// 拖拽开始时的鼠标位置
        /// </summary>
        private Point _dragStartPoint;
        /// <summary>
        /// 是否正在进行拖拽操作
        /// </summary>
        private bool _isDragging;
        /// <summary>
        /// 拖拽阈值（超过此距离才认为是拖拽操作）
        /// </summary>
        private const double DragThreshold = 4.0;

        /// <summary>
        /// 构造函数
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel();
            KeyDown += OnWindowKeyDown;
        }

        /// <summary>
        /// 窗口键盘按下事件，按Enter键时移除焦点
        /// </summary>
        private void OnWindowKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Avalonia.Input.Key.Enter)
            {
                FocusManager?.ClearFocus();
                e.Handled = true;
            }
        }

        /// <summary>
        /// 切换正则模式按钮点击事件
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">路由事件参数</param>
        private void ToggleRegexMode_Click(object? sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                vm.IsRegexMode = !vm.IsRegexMode;
            }
        }

        /// <summary>
        /// 数据上下文变更事件
        /// </summary>
        /// <param name="e">事件参数</param>
        protected override void OnDataContextChanged(EventArgs e)
        {
            base.OnDataContextChanged(e);
#if DEBUG
            if (DataContext is MainViewModel vm)
            {
                vm.SetFolderPath("c:\\aaa\\bbb");
            }
#endif
        }

        /// <summary>
        /// 字幕项的鼠标按下事件（通过 DataTemplate 调用）
        /// 初始化拖拽操作
        /// </summary>
        /// <param name="sender">事件发送者（字幕项的 Border 控件）</param>
        /// <param name="e">鼠标按下事件参数</param>
        public void OnSubtitlePointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed == false) return;
            if (sender is not Control control) return;
            if (control.DataContext is not SubtitleItem subtitle) return;

            _draggingSubtitle = subtitle;
            _draggingSourceGroup = subtitle.ParentGroup;
            _dragStartPoint = e.GetPosition(this);
            _isDragging = false;

            // 注册鼠标移动和释放事件
            PointerMoved += OnPointerMovedDrag;
            PointerReleased += OnPointerReleasedDrag;

            e.Handled = true;
        }

        /// <summary>
        /// 鼠标移动事件（拖拽过程中）
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">鼠标事件参数</param>
        private void OnPointerMovedDrag(object? sender, PointerEventArgs e)
        {
            if (_draggingSubtitle == null) return;

            var currentPos = e.GetPosition(this);
            var diff = currentPos - _dragStartPoint;

            // 判断是否超过拖拽阈值
            if (!_isDragging)
            {
                if (Math.Abs(diff.X) < DragThreshold && Math.Abs(diff.Y) < DragThreshold)
                    return;

                // 开始拖拽
                _isDragging = true;
                _draggingSubtitle.IsDragging = true;
                CreateDragGhost();
            }

            if (_isDragging)
            {
                // 更新幽灵控件位置
                UpdateDragGhostPosition(currentPos);
                // 查找鼠标下方的目标组
                var hoverGroup = FindGroupUnderMouse(currentPos);
                if (DataContext is MainViewModel vm)
                {
                    vm.SetDragOverGroup(hoverGroup);
                }
            }
        }

        /// <summary>
        /// 创建拖拽幽灵控件（跟随鼠标的预览）
        /// </summary>
        private void CreateDragGhost()
        {
            if (_draggingSubtitle == null) return;

            _draggingGhost = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(0, 120, 215)),
                BorderBrush = Brushes.White,
                BorderThickness = new Thickness(2),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(8, 4),
                Opacity = 0.9,
                IsHitTestVisible = false,
                Child = new TextBlock
                {
                    Text = _draggingSubtitle.Name,
                    Foreground = Brushes.White,
                    FontSize = 13
                }
            };

            // 找到 Window 的内容 Panel
            if (this.Content is Panel panel)
            {
                _dragGhostCanvas = new Canvas { IsHitTestVisible = false };
                panel.Children.Add(_dragGhostCanvas);
            }
            else
            {
                // 包裹一层
                var wrapper = new Panel { IsHitTestVisible = false };
                if (this.Content is Control originalContent)
                {
                    this.Content = null;
                    wrapper.Children.Add(originalContent);
                    wrapper.Children.Add(_dragGhostCanvas = new Canvas());
                    this.Content = wrapper;
                }
            }

            _dragGhostCanvas?.Children.Add(_draggingGhost);
        }

        /// <summary>
        /// 更新拖拽幽灵控件的位置
        /// </summary>
        /// <param name="position">当前鼠标位置</param>
        private void UpdateDragGhostPosition(Point position)
        {
            if (_draggingGhost == null || _dragGhostCanvas == null) return;

            Canvas.SetLeft(_draggingGhost, position.X + 10);
            Canvas.SetTop(_draggingGhost, position.Y + 10);
        }

        /// <summary>
        /// 查找鼠标位置下方的文件匹配组
        /// 通过遍历视觉树找到对应的 FileMatchGroup
        /// </summary>
        /// <param name="position">鼠标位置</param>
        /// <returns>找到的文件匹配组，未找到返回 null</returns>
        private FileMatchGroup? FindGroupUnderMouse(Point position)
        {
            var element = this.InputHitTest(position) as Visual;
            if (element == null) return null;

            Visual? current = element;
            while (current != null)
            {
                if (current is Border border && border.DataContext is FileMatchGroup group)
                {
                    return group;
                }
                current = current.GetVisualParent();
            }
            return null;
        }

        /// <summary>
        /// 鼠标释放事件（拖拽结束）
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">鼠标释放事件参数</param>
        private void OnPointerReleasedDrag(object? sender, PointerReleasedEventArgs e)
        {
            // 注销鼠标移动和释放事件
            PointerMoved -= OnPointerMovedDrag;
            PointerReleased -= OnPointerReleasedDrag;

            // 处理拖拽结束逻辑
            if (_isDragging && DataContext is MainViewModel vm)
            {
                var hoverGroup = FindGroupUnderMouse(e.GetPosition(this));
                if (hoverGroup != null && _draggingSubtitle != null && _draggingSourceGroup != null)
                {
                    vm.MoveSubtitle(_draggingSourceGroup, hoverGroup, _draggingSubtitle);
                }
                else
                {
                    vm.ClearDragStates();
                }
            }

            // 清理幽灵控件
            if (_dragGhostCanvas != null)
            {
                if (_draggingGhost != null)
                    _dragGhostCanvas.Children.Remove(_draggingGhost);

                var parent = _dragGhostCanvas.Parent as Panel;
                if (parent != null && _dragGhostCanvas.Children.Count == 0)
                {
                    parent.Children.Remove(_dragGhostCanvas);
                }
            }

            // 重置拖拽状态
            _draggingGhost = null;
            _dragGhostCanvas = null;
            _draggingSubtitle = null;
            _draggingSourceGroup = null;
            _isDragging = false;
        }

        #region 外部文件夹拖入

        /// <summary>
        /// 外部拖拽进入窗口（来自系统资源管理器）
        /// </summary>
        private void OnWindowDragEnter(object? sender, DragEventArgs e)
        {
            if (TryGetExternalFolderPath(e, out _))
            {
                e.DragEffects = DragDropEffects.Copy;
            }
            else
            {
                e.DragEffects = DragDropEffects.None;
            }
        }

        /// <summary>
        /// 外部拖拽在窗口上移动
        /// </summary>
        private void OnWindowDragOver(object? sender, DragEventArgs e)
        {
            if (TryGetExternalFolderPath(e, out _))
            {
                e.DragEffects = DragDropEffects.Copy;
            }
            else
            {
                e.DragEffects = DragDropEffects.None;
            }
        }

        /// <summary>
        /// 外部拖拽离开窗口
        /// </summary>
        private void OnWindowDragLeave(object? sender, DragEventArgs e)
        {
        }

        /// <summary>
        /// 外部拖拽释放到窗口：取第一个文件夹路径，写入 FolderPath 并触发加载
        /// </summary>
        private async void OnWindowDrop(object? sender, DragEventArgs e)
        {
            if (!TryGetExternalFolderPath(e, out var path) || path == null) return;
            if (DataContext is not MainViewModel vm) return;

            vm.FolderPath = path;
            await vm.LoadFilesFromExternalAsync();
        }

        /// <summary>
        /// 判断拖入数据中是否包含文件夹路径（含则返回第一个有效路径）
        /// </summary>
        private static bool TryGetExternalFolderPath(DragEventArgs e, out string? path)
        {
            path = null;
            if (!e.Data.Contains(DataFormats.Files)) return false;
            var files = e.Data.GetFiles();
            if (files == null) return false;

            foreach (var item in files)
            {
                var localPath = item.TryGetLocalPath();
                if (string.IsNullOrEmpty(localPath)) continue;
                if (System.IO.Directory.Exists(localPath))
                {
                    path = localPath;
                    return true;
                }
            }
            return false;
        }

        #endregion


    }
}

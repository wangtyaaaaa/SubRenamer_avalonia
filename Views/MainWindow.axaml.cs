using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.VisualTree;
using SubRenamer.ViewModels;

namespace SubRenamer.Views
{
    public partial class MainWindow : Window
    {
        private SubtitleItem? _draggingSubtitle;
        private FileMatchGroup? _draggingSourceGroup;
        private Border? _draggingGhost;
        private Canvas? _dragGhostCanvas;
        private Point _dragStartPoint;
        private bool _isDragging;
        private const double DragThreshold = 4.0;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel();
        }

        private void ToggleRegexMode_Click(object? sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                vm.IsRegexMode = !vm.IsRegexMode;
            }
        }

        protected override void OnDataContextChanged(EventArgs e)
        {
            base.OnDataContextChanged(e);
            if (DataContext is MainViewModel vm)
            {
#if DEBUG
                vm.SetFolderPath("c:\\aaa\\bbb");
#elif RELEASE
                vm.BrowseFolderCommand = new RelayCommand(async () => await BrowseFolderAsync(vm), () => true);
#endif
            }
        }

        // 字幕项的 PointerPressed 事件（通过 DataTemplate 调用）
        public void OnSubtitlePointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed == false) return;
            if (sender is not Control control) return;
            if (control.DataContext is not SubtitleItem subtitle) return;

            _draggingSubtitle = subtitle;
            _draggingSourceGroup = subtitle.ParentGroup;
            _dragStartPoint = e.GetPosition(this);
            _isDragging = false;

            PointerMoved += OnPointerMovedDrag;
            PointerReleased += OnPointerReleasedDrag;

            e.Handled = true;
        }

        private void OnPointerMovedDrag(object? sender, PointerEventArgs e)
        {
            if (_draggingSubtitle == null) return;

            var currentPos = e.GetPosition(this);
            var diff = currentPos - _dragStartPoint;

            if (!_isDragging)
            {
                if (Math.Abs(diff.X) < DragThreshold && Math.Abs(diff.Y) < DragThreshold)
                    return;

                _isDragging = true;
                _draggingSubtitle.IsDragging = true;
                CreateDragGhost();
            }

            if (_isDragging)
            {
                UpdateDragGhostPosition(currentPos);
                var hoverGroup = FindGroupUnderMouse(currentPos);
                if (DataContext is MainViewModel vm)
                {
                    vm.SetDragOverGroup(hoverGroup);
                }
            }
        }

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

        private void UpdateDragGhostPosition(Point position)
        {
            if (_draggingGhost == null || _dragGhostCanvas == null) return;

            Canvas.SetLeft(_draggingGhost, position.X + 10);
            Canvas.SetTop(_draggingGhost, position.Y + 10);
        }

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

        private void OnPointerReleasedDrag(object? sender, PointerReleasedEventArgs e)
        {
            PointerMoved -= OnPointerMovedDrag;
            PointerReleased -= OnPointerReleasedDrag;

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

            // 清理幽灵
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

            _draggingGhost = null;
            _dragGhostCanvas = null;
            _draggingSubtitle = null;
            _draggingSourceGroup = null;
            _isDragging = false;
        }

#if RELEASE
        private async System.Threading.Tasks.Task BrowseFolderAsync(MainViewModel vm)
        {
            var topLevel = GetTopLevel(this);
            if (topLevel == null) return;

            var result = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "选择文件夹",
                AllowMultiple = false
            });

            if (result.Count > 0)
            {
                var path = result[0].Path.LocalPath;
                vm.SetFolderPath(path);
            }
        }
#endif
    }
}

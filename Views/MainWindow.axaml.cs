using Avalonia.Controls;
using Avalonia.Interactivity;
using SubRenamer.ViewModels;

namespace SubRenamer.Views
{
    public partial class MainWindow : Window
    {
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

using Avalonia;
using Avalonia.Controls;
using yt_downloader.ViewModels;

namespace yt_downloader.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.DataContext = new MainWindowViewModel(this);
            InitializeComponent();
        }
    }
}

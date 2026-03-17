using MDownloader.ViewModels;
using System.Windows;
using System.Windows.Input;

namespace MDownloader;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        VideoPlayerControl.MediaOpened += (s, e) => LblDownloadStatus.Text = "Воспроизведение...";
        VideoPlayerControl.MediaFailed += (s, e) => LblDownloadStatus.Text = "Ошибка воспроизведения";
    }

    protected override void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);
        if (DataContext is MainViewModel vm)
            vm.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(MainViewModel.CurrentVideoPath))
                {
                    VideoPlayerControl.Source = new Uri(vm.CurrentVideoPath);
                    VideoPlayerControl.Play();
                }
            };
    }
}
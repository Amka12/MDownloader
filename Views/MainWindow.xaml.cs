using System.Windows;
using MDownloader.ViewModels;

namespace MDownloader;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
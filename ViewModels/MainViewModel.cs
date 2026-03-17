using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MDownloader.Models;
using MDownloader.Services;
using Microsoft.Win32;

namespace MDownloader.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IFileService _fileService;
    [ObservableProperty] private string _currentVideoPath = string.Empty;

    [ObservableProperty] private string _folderPath = "Не выбрана";
    [ObservableProperty] private VideoFile? _selectedVideo;

    public MainViewModel(IFileService fileService)
    {
        _fileService = fileService;
        _fileService.FilesChanged += OnFilesChanged;
    }

    public ObservableCollection<VideoFile> VideoFiles { get; } = new();

    private void OnFilesChanged()
    {
        var files = _fileService.GetVideoFiles();
        Application.Current.Dispatcher.Invoke(() =>
        {
            VideoFiles.Clear();
            foreach (var f in files) VideoFiles.Add(f);
        });
    }

    [RelayCommand]
    private void OpenFolder()
    {
        var dialog = new OpenFolderDialog { Title = "Выберете папку с видео" };
        if (dialog.ShowDialog() == true)
        {
            _fileService.SelectedFolderPath = dialog.FolderName;
            FolderPath = dialog.FolderName;
            _fileService.RefreshFiles();
        }
    }

    [RelayCommand]
    private void RefreshList()
    {
        _fileService.RefreshFiles();
    }

    [RelayCommand]
    private void PlayVideo()
    {
        if (SelectedVideo != null) CurrentVideoPath = SelectedVideo.FullPath;
    }

    [RelayCommand(CanExecute = nameof(CanPlayVideo))]
    private void PlaySelectedVideo() => PlayVideo();

    private bool CanPlayVideo() => SelectedVideo != null;
}
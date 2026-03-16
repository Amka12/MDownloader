using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MDownloader.Models;
using MDownloader.Services;
using Microsoft.Win32;

namespace MDownloader.ViewModels;

public partial class MainViewModel:ObservableObject
{
    private readonly IFileService _fileService;

    [ObservableProperty] private string _folderPath = "Не выбрана";
    [ObservableProperty] private VideoFile? _selectedVideo;
    [ObservableProperty] private string _currentVideoPath = string.Empty;

    public ObservableCollection<VideoFile> VideoFiles { get; } = new();

    public MainViewModel(IFileService fileService)
    {
        _fileService= fileService;
        _fileService.FilesChanged += OnFilesChanged;
    }

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
    private void RefreshList() => _fileService.RefreshFiles();
}
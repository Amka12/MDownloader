using MDownloader.Models;

namespace MDownloader.Services;

public interface IFileService
{
    string? SelectedFolderPath { get; set; }
    List<VideoFile> GetVideoFiles();
    void RefreshFiles();
    event Action? FilesChanged;
}

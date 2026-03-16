using System.IO;
using MDownloader.Models;

namespace MDownloader.Services;

public class FileService : IFileService
{
    private static readonly string[] VideoExtensions = { ".mp4", ".avi", ".mkv", ".mov", ".wmv", ".flv", ".webm" };
    public string? SelectedFolderPath { get; set; }

    public event Action? FilesChanged;

    public List<VideoFile> GetVideoFiles()
    {
        if (string.IsNullOrEmpty(SelectedFolderPath) || !Directory.Exists(SelectedFolderPath))
            return new List<VideoFile>();

        return Directory.GetFiles(SelectedFolderPath)
            .Where(f => VideoExtensions.Contains(Path.GetExtension(f).ToLower()))
            .Select(f => new VideoFile
            {
                Name = Path.GetFileName(f),
                FullPath = f,
                Extension = Path.GetExtension(f),
                Size = FormatFileSize(new FileInfo(f).Length),
                Date = File.GetLastWriteTime(f).ToString("dd.MM.yyyy HH:mm")
            })
            .OrderBy(f => f.Name)
            .ToList();
    }

    public void RefreshFiles()
    {
        FilesChanged?.Invoke();
    }

    private static string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        var order = 0;
        double size = bytes;
        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }

        return $"{size:0.##} {sizes[order]}";
    }
}
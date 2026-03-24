namespace MDownloader.Models;

public class DownloadTask
{
    public string Url { get; set; } = string.Empty;
    public string Quality { get; set; } = "1080p";
    public int Progress { get; set; }
    public string Status { get; set; } = "Ожидание";
    public bool IsCompleted { get; set; }
    public string FileName { get; set; } = string.Empty;
}
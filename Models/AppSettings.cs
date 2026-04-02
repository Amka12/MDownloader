namespace MDownloader.Models;

public class AppSettings
{
    public string? LastFolderPath { get; set; }
    public double Volume { get; set; } = 50;
    public bool IsMuted { get; set; } = false;
}
namespace MDownloader.Models;

public class AppSettings
{
    public string? LastFolderPath { get; set; }
    public double Volume { get; set; } = 50;
    public bool IsMuted { get; set; } = false;

    public double WindowWidth { get; set; } = 1200;
    public double WindowHeight { get; set; } = 800;
    public double WindowLeft { get; set; } = 0;
    public double WindowTop { get; set; } = 0;
    public bool IsMaximized { get; set; } = false;
}
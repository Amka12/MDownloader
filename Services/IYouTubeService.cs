namespace MDownloader.Services;

public interface IYouTubeService
{
    Task<DownloadResult> DownloadVideoAsync(string url, string quality, string savePath, IProgress<double>? progress = null, CancellationToken ct = default);
}
using System.IO;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;

namespace MDownloader.Services;

public class DownloadResult
{
    public bool Success { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public string Error { get; set; } = string.Empty;
}

public class YouTubeService : IYouTubeService
{
    public async Task<DownloadResult> DownloadVideoAsync(string url, string quality, string savePath, IProgress<double>? progress = null, CancellationToken ct = default)
    {
        try
        {
            var youtube = new YoutubeClient();
            var video = await youtube.Videos.GetAsync(url, ct);
            var streamManifest = await youtube.Videos.Streams.GetManifestAsync(video.Id, ct);
            var streamInfo = quality switch
            {
                "720p" => streamManifest.GetVideoStreams().FirstOrDefault(s => s.VideoResolution.Height == 720),
                "Audio Only" => streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate(),
                _ => streamManifest.GetVideoStreams().GetWithHighestVideoQuality()
            };

            if (streamInfo == null)
                return new DownloadResult { Success = false, Error = "Не удалось найти поток нужного качества" };

            var fileName = $"{video.Title}.{streamInfo.Container}";
            var fullPath = Path.Combine(savePath, fileName);
            var counter = 1;
            while (File.Exists(fullPath))
            {
                fullPath = Path.Combine(savePath, $"{video.Title}_{counter}.{streamInfo.Container}");
                counter++;
            }

            await youtube.Videos.Streams.DownloadAsync(streamInfo, fullPath, progress, ct);
            return new DownloadResult { Success = true, FilePath = fullPath };
        }
        catch (Exception ex)
        {
            return new DownloadResult { Success = false, Error = ex.Message };
        }
    }
}
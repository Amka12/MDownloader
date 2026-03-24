using System.IO;
using YoutubeExplode;
using YoutubeExplode.Converter;
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
                "1080p" => streamManifest.GetVideoStreams().FirstOrDefault(s => s.VideoResolution.Height == 1080 && s.Container == Container.Mp4),
                "720p" => streamManifest.GetVideoStreams().FirstOrDefault(s => s.VideoResolution.Height == 720 && s.Container == Container.Mp4),
                "Audio Only" => streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate(),
                _ => streamManifest.GetVideoStreams().GetWithHighestVideoQuality()
            };

            if (streamInfo == null)
                return new DownloadResult { Success = false, Error = "Не удалось найти поток нужного качества" };

            var audioStreamInfo = streamManifest
                .GetAudioStreams()
                .Where(s => s.Container == Container.Mp4)
                .GetWithHighestBitrate();

            if (audioStreamInfo == null)
                return new DownloadResult { Success = false, Error = "Не удалось найти аудио поток" };

            var outputFileName = SanitizeFilename(video.Title);
            var fileName = $"{outputFileName}.{streamInfo.Container}";
            var fullPath = Path.Combine(savePath, fileName);
            var counter = 1;
            while (File.Exists(fullPath))
            {
                fullPath = Path.Combine(savePath, $"{video.Title}_{counter}.{streamInfo.Container}");
                counter++;
            }

            await youtube.Videos.DownloadAsync([audioStreamInfo, streamInfo], new ConversionRequestBuilder(fullPath).Build(), progress, ct);
            return new DownloadResult { Success = true, FilePath = fullPath };
        }
        catch (Exception ex)
        {
            return new DownloadResult { Success = false, Error = ex.Message };
        }
    }

    private static string SanitizeFilename(string filename)
    {
        if (string.IsNullOrWhiteSpace(filename)) return $"video{Random.Shared.Next()}";

        filename = Path.GetInvalidFileNameChars().Aggregate(filename, (current, c) => current.Replace(c.ToString(), ""));

        if (string.IsNullOrWhiteSpace(filename)) return $"video{Random.Shared.Next()}";

        const int maxFileNameLength = 200;

        if (filename.Length > maxFileNameLength) filename = filename.Substring(0, maxFileNameLength);

        return filename;
    }
}
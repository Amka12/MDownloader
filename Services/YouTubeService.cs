using System.Diagnostics;
using System.IO;
using YoutubeExplode;
using YoutubeExplode.Videos;
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
    private readonly string _ffmpegPath;

    public YouTubeService()
    {
        // Путь к ffmpeg.exe (должен лежать в папке с exe)
        _ffmpegPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ffmpeg", "bin", "ffmpeg.exe");

        if (!File.Exists(_ffmpegPath)) _ffmpegPath = "ffmpeg";
    }

    public async Task<DownloadResult> DownloadVideoAsync(string url, string quality, string savePath, IProgress<double>? progress = null, CancellationToken ct = default)
    {
        try
        {
            var youtube = new YoutubeClient();
            var video = await youtube.Videos.GetAsync(url, ct);
            var streamManifest = await youtube.Videos.Streams.GetManifestAsync(video.Id, ct);

            // Для 720p+ скачиваем видео и аудио отдельно
            var needsMerge = quality is "1080p" or "1440p" or "2160p";

            if (needsMerge) return await DownloadWithMergeAsync(youtube, video, streamManifest, quality, savePath, progress, ct);

            return await DownloadCombinedAsync(youtube, video, streamManifest, quality, savePath, progress, ct);
        }
        catch (Exception ex)
        {
            return new DownloadResult { Success = false, Error = ex.Message };
        }
    }

    private async Task<DownloadResult> DownloadWithMergeAsync(YoutubeClient youtube, Video video, StreamManifest streamManifest, string quality, string savePath, IProgress<double>? progress, CancellationToken ct)
    {
        // Выбираем видеопоток нужного качества
        var videoStream = quality switch
        {
            "1080p" => streamManifest.GetVideoStreams().FirstOrDefault(s => s.VideoResolution.Height == 1080),
            "1440p" => streamManifest.GetVideoStreams().FirstOrDefault(s => s.VideoResolution.Height == 1440),
            "2160p" => streamManifest.GetVideoStreams().FirstOrDefault(s => s.VideoResolution.Height == 2160),
            _ => streamManifest.GetVideoStreams().GetWithHighestVideoQuality()
        };

        // Выбираем лучший аудиопоток
        var audioStream = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();

        if (videoStream == null || audioStream == null)
            return new DownloadResult { Success = false, Error = "Не удалось найти потоки нужного качества" };

        // Временные файлы
        var tempVideoPath = Path.Combine(savePath, $"temp_video_{video.Id}.mp4");
        var tempAudioPath = Path.Combine(savePath, $"temp_audio_{video.Id}.m4a");
        var finalPath = Path.Combine(savePath, $"{SanitizeFilename(video.Title)}.mp4");

        // Скачиваем видео
        await youtube.Videos.Streams.DownloadAsync(videoStream, tempVideoPath, progress, ct);

        // Скачиваем аудио
        await youtube.Videos.Streams.DownloadAsync(audioStream, tempAudioPath, null, ct);

        // Merge через FFmpeg
        var ffmpegArgs = $"-i \"{tempVideoPath}\" -i \"{tempAudioPath}\" -c copy \"{finalPath}\" -y";
        var processInfo = new ProcessStartInfo
        {
            FileName = _ffmpegPath,
            Arguments = ffmpegArgs,
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        using var process = Process.Start(processInfo);
        if (process != null) await process.WaitForExitAsync(ct);

        // Удаляем временные файлы
        try
        {
            File.Delete(tempVideoPath);
            File.Delete(tempAudioPath);
        }
        catch
        {
        }

        return new DownloadResult { Success = true, FilePath = finalPath };
    }

    private async Task<DownloadResult> DownloadCombinedAsync(YoutubeClient youtube, Video video, StreamManifest streamManifest, string quality, string savePath, IProgress<double>? progress, CancellationToken ct)
    {
        var streamInfo = quality switch
        {
            "720p" => streamManifest.GetVideoStreams().FirstOrDefault(s => s.VideoResolution.Height == 720),
            "360p" => streamManifest.GetVideoStreams().FirstOrDefault(s => s.VideoResolution.Height == 360),
            "Audio Only" => streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate(),
            _ => streamManifest.GetVideoStreams().GetWithHighestVideoQuality()
        };

        if (streamInfo == null)
            return new DownloadResult { Success = false, Error = "Не удалось найти поток нужного качества" };

        var fileName = $"{SanitizeFilename(video.Title)}.{streamInfo.Container}";
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
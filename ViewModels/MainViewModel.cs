using AngleSharp.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LibVLCSharp.Shared;
using MDownloader.Models;
using MDownloader.Services;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Threading;

namespace MDownloader.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IFileService _fileService;
    private readonly IYouTubeService _youtubeService;

    private readonly LibVLC? _libVlc;
    private bool _isUserDraggingSlider;
    private DispatcherTimer? _progressTimer;

    [ObservableProperty] private double _currentPosition;
    [ObservableProperty] private string _currentTime = "00:00";
    [ObservableProperty] private string _currentVideoPath = string.Empty;
    [ObservableProperty] private int _downloadProgress;
    [ObservableProperty] private string _downloadStatus = "Ожидание...";
    [ObservableProperty] private double _duration;
    [ObservableProperty] private string _folderPath = "Не выбрана";
    [ObservableProperty] private bool _isDownloading;
    [ObservableProperty] private bool _isMuted;
    [ObservableProperty] private bool _isPaused;
    [ObservableProperty] private bool _isPlaying;

    //Player statements
    [ObservableProperty] private MediaPlayer? _mediaPlayer;
    [ObservableProperty] private string _selectedQuality = "1080p";
    [ObservableProperty] private VideoFile? _selectedVideo;
    [ObservableProperty] private string _totalTime = "00:00";
    [ObservableProperty] private double _volume = 50;
    [ObservableProperty] private string _youtubeUrl = string.Empty;

    // --- Свойства для информации о видео ---
    [ObservableProperty] private string _audioInfo = string.Empty;
    [ObservableProperty] private string _videoInfo = string.Empty;
    [ObservableProperty] private string _videoCodec = string.Empty;
    [ObservableProperty] private string _audioCodec = string.Empty;
    [ObservableProperty] private bool _hasVideoInfo;

    public MainViewModel(IFileService fileService, IYouTubeService youtubeService)
    {
        _fileService = fileService;
        _youtubeService = youtubeService;
        _fileService.FilesChanged += OnFilesChanged;
        Core.Initialize();
        _libVlc = new LibVLC();
        MediaPlayer = new MediaPlayer(_libVlc);
        MediaPlayer.EnableHardwareDecoding = true;
        InitializePlayer(MediaPlayer);
        OnVolumeChanged(Volume);
    }

    public List<string> QualityOptions { get; } = new()
    {
        "1080p", "720p" /*, "Audio Only"*/
    };

    public ObservableCollection<VideoFile> VideoFiles { get; } = new();

    public void InitializePlayer(MediaPlayer mediaPlayer)
    {
        if (MediaPlayer == null) return;

        MediaPlayer.MediaChanged += async (s, e) =>
        {
            await LoadMediaInfo();
        };
        MediaPlayer.Playing += (s, e) =>
        {
            IsPlaying = true;
            IsPaused = true;
        };
        MediaPlayer.Paused += (s, e) =>
        {
            IsPlaying = false;
            IsPaused = true;
        };
        MediaPlayer.Stopped += (s, e) =>
        {
            IsPlaying = false;
            IsPaused = false;
            CurrentPosition = 0;
            CurrentTime = "00:00";
            HasVideoInfo = false;
        };
        MediaPlayer.EndReached += (s, e) =>
        {
            IsPlaying = false;
            IsPaused = false;
        };
        MediaPlayer.LengthChanged += (s, e) =>
        {
            Duration = e.Length / 1000.0;
            TotalTime = FormatTime(TimeSpan.FromMilliseconds(e.Length));
        };

        _progressTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(500)
        };
        _progressTimer.Tick += (s, e) => UpdateProgress();
        _progressTimer.Start();
    }

    #region partialHandlers
    partial void OnVolumeChanged(double value)
    {
        if (MediaPlayer != null) MediaPlayer.Volume = (int)Math.Max(0, Math.Min(100, value));
    }

    partial void OnIsMutedChanged(bool value)
    {
        if (MediaPlayer != null)
            MediaPlayer.Mute = value;
    }
    #endregion

    private void UpdateProgress()
    {
        if (MediaPlayer != null && IsPlaying && !_isUserDraggingSlider)
        {
            CurrentPosition = MediaPlayer.Time / 1000.0;
            CurrentTime = FormatTime(TimeSpan.FromMilliseconds(MediaPlayer.Time));
        }
    }

    private string FormatTime(TimeSpan time)
    {
        return $"{(int)time.TotalMinutes:00}:{time.Seconds:00}";
    }

    private async Task LoadMediaInfo()
    {
        if (MediaPlayer == null || MediaPlayer.Media == null) return;

        await Application.Current.Dispatcher.InvokeAsync(async () =>
        {
            try
            {
                await MediaPlayer.Media.Parse();
                foreach (var track in MediaPlayer.Media.Tracks)
                {
                    switch (track.TrackType)
                    {
                        case TrackType.Audio:
                            AudioInfo = $"{track.Bitrate/1000} kbps, {track.Data.Audio.Channels}x{track.Data.Audio.Rate} Hz";
                            AudioCodec= MediaPlayer.Media.CodecDescription(TrackType.Audio, track.Codec);
                            break;
                        case TrackType.Video:
                            VideoInfo = $"{track.Data.Video.Width}x{track.Data.Video.Height} {track.Data.Video.FrameRateNum/track.Data.Video.FrameRateDen} к/с";
                            VideoCodec = MediaPlayer.Media.CodecDescription(TrackType.Video, track.Codec);
                            break;
                    }
                }

                HasVideoInfo = true;
            }
            catch (Exception e)
            {
                VideoInfo = $"Ошибка: {e.Message}";
                HasVideoInfo = false;
            }
        });
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

    #region Commands
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
    private void RefreshList()
    {
        _fileService.RefreshFiles();
    }

    [RelayCommand]
    private async void PlayVideo(VideoFile? file)
    {
        var targetFile = file ?? SelectedVideo;
        if (targetFile != null && MediaPlayer != null)
        {
            CurrentVideoPath = targetFile.FullPath;
            var media = new Media(_libVlc, CurrentVideoPath);

            await media.Parse(MediaParseOptions.ParseLocal);
            foreach (var track in media.Tracks)
            {
                switch (track.TrackType)
                {
                    case TrackType.Audio:
                        Debug.WriteLine("Audio track");
                        Debug.WriteLine($"{nameof(track.Data.Audio.Channels)}: {track.Data.Audio.Channels}");
                        Debug.WriteLine($"{nameof(track.Data.Audio.Rate)}: {track.Data.Audio.Rate}");
                        break;
                    case TrackType.Video:
                        Debug.WriteLine("Video track");
                        Debug.WriteLine($"{nameof(track.Data.Video.FrameRateNum)}: {track.Data.Video.FrameRateNum}");
                        Debug.WriteLine($"{nameof(track.Data.Video.FrameRateDen)}: {track.Data.Video.FrameRateDen}");
                        Debug.WriteLine($"{nameof(track.Data.Video.Height)}: {track.Data.Video.Height}");
                        Debug.WriteLine($"{nameof(track.Data.Video.Width)}: {track.Data.Video.Width}");
                        break;
                }
            }
            
            MediaPlayer.Media = media;
            MediaPlayer.Play();
        }
    }

    [RelayCommand]
    private void Play()
    {
        MediaPlayer?.Play();
    }

    [RelayCommand]
    private void Pause()
    {
        MediaPlayer?.Pause();
    }

    [RelayCommand]
    private void Stop()
    {
        MediaPlayer?.Stop();
        CurrentPosition = 0;
        CurrentTime = "00:00";
        IsPlaying = false;
        IsPaused = false;
    }

    [RelayCommand]
    private void TogglePlayPause()
    {
        if (MediaPlayer == null) return;

        if (MediaPlayer.IsPlaying)
            MediaPlayer.Pause();
        else if (IsPaused)
            MediaPlayer.Play();
        else
            PlayVideo(SelectedVideo);
    }

    [RelayCommand]
    private void Seek(double position)
    {
        MediaPlayer?.Time = (long)(position * 1000);
    }

    [RelayCommand]
    public void SetDragging(string isDragging)
    {
        _isUserDraggingSlider = Convert.ToBoolean(isDragging);
    }

    [RelayCommand]
    private async Task DownloadAsync()
    {
        if (string.IsNullOrWhiteSpace(YoutubeUrl) || string.IsNullOrEmpty(_fileService.SelectedFolderPath))
        {
            MessageBox.Show("Укажите ссылку и выберите папку!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        IsDownloading = true;
        DownloadStatus = "Подготовка...";
        DownloadProgress = 0;

        var progress = new Progress<double>(p =>
        {
            DownloadProgress = (int)(p * 100);
            DownloadStatus = $"Загрузка: {DownloadProgress}%";
        });

        var result = await _youtubeService.DownloadVideoAsync(YoutubeUrl, SelectedQuality, _fileService.SelectedFolderPath, progress);

        if (result.Success)
        {
            DownloadStatus = "Загрузка завершена!";
            _fileService.RefreshFiles();
            YoutubeUrl = string.Empty;
        }
        else
        {
            DownloadStatus = $"Ошибка: {result.Error}";
        }

        IsDownloading = false;
    }

    #endregion

    // Очистка ресурсов
    public void Dispose()
    {
        _progressTimer?.Stop();
        MediaPlayer?.Dispose();
        _libVlc?.Dispose();
    }
}
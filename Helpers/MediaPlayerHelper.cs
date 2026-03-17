using LibVLCSharp.Shared;
using System.Windows;

namespace MDownloader.Helpers;

public static class MediaPlayerHelper
{
    public static readonly DependencyProperty MediaPlayerProperty =
        DependencyProperty.RegisterAttached(
            "MediaPlayer",
            typeof(MediaPlayer),
            typeof(MediaPlayerHelper),
            new PropertyMetadata(null, OnMediaPlayerChanged));

    public static void SetMediaPlayer(DependencyObject element, MediaPlayer value)
    {
        element.SetValue(MediaPlayerProperty, value);
    }

    public static MediaPlayer GetMediaPlayer(DependencyObject element)
    {
        return (MediaPlayer)element.GetValue(MediaPlayerProperty);
    }

    private static void OnMediaPlayerChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is LibVLCSharp.WPF.VideoView videoView && e.NewValue is MediaPlayer mediaPlayer)
        {
            videoView.MediaPlayer = mediaPlayer;
        }
    }
}
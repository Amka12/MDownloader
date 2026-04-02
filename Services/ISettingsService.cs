using MDownloader.Models;

namespace MDownloader.Services;

public interface ISettingsService
{
    AppSettings LoadSettings();
    void SaveSettings(AppSettings  settings);
    string SettingsFilePath { get; }
}
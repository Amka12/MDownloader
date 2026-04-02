using System.Diagnostics;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using MDownloader.Models;

namespace MDownloader.Services;

internal class SettingsService : ISettingsService
{
    private readonly string _settingsFileName = "appsettings.json";

    public string SettingsFilePath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _settingsFileName);

    public AppSettings LoadSettings()
    {
        try
        {
            if (File.Exists(SettingsFilePath))
            {
                var json = File.ReadAllText(SettingsFilePath);
                var settings = JsonSerializer.Deserialize<AppSettings>(json);
                return settings ?? new AppSettings();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Ошибка загрузки настроек: {ex.Message}");
        }

        return new AppSettings();
    }

    public void SaveSettings(AppSettings settings)
    {
        try
        {
            //var directory = Path.GetDirectoryName(SettingsFilePath);
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
            var json = JsonSerializer.Serialize(settings, options);

            File.WriteAllText(SettingsFilePath, json);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Ошибка сохранения настроек: {ex.Message}");
        }
    }
}
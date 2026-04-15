using System;
using System.IO;
using System.Text.Json;
using theLinkType.Models;

namespace theLinkType.Services;

public sealed class SettingsService
{
    private readonly string _settingsPath;

    public SettingsService()
    {
        string folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "theLinkType");

        Directory.CreateDirectory(folder);
        _settingsPath = Path.Combine(folder, "settings.json");
    }

    public AppSettings Load()
    {
        if (!File.Exists(_settingsPath))
            return new AppSettings();

        try
        {
            string json = File.ReadAllText(_settingsPath);
            return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    public void Save(AppSettings settings)
    {
        string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        File.WriteAllText(_settingsPath, json);
    }
}
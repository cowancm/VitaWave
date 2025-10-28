using Serilog;
using System;
using System.IO;
using System.Text.Json;

namespace VitaWave.WebAPI.Settings
{
    public static class SettingsManager
    {
        private static readonly string _folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "vitawave"
        );

        private const string FILE_NAME = "settings.json";
        private static readonly string _filePath = Path.Combine(_folder, FILE_NAME);

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = true
        };

        public static Settings GetSettings()
        {
            try
            {
                if (!Directory.Exists(_folder))
                    Directory.CreateDirectory(_folder);

                if (!File.Exists(_filePath))
                {
                    var defaultSettings = new Settings();
                    SaveSettings(defaultSettings);
                    return defaultSettings;
                }

                string json = File.ReadAllText(_filePath);
                var settings = JsonSerializer.Deserialize<Settings>(json, _jsonOptions);

                return settings ?? new Settings();
            }
            catch (Exception ex)
            {
                Log.Error($"[SettingsManager] Error reading settings: {ex.Message}");
                return new Settings();
            }
        }

        public static void SaveSettings(Settings settings)
        {
            try
            {
                if (!Directory.Exists(_folder))
                    Directory.CreateDirectory(_folder);

                string json = JsonSerializer.Serialize(settings, _jsonOptions);
                File.WriteAllText(_filePath, json);
            }
            catch (Exception ex)
            {
                Log.Error($"[SettingsManager] Error saving settings: {ex.Message}");
            }
        }
    }

    public class Settings
    {
        public TextSettings TextSettings { get; set; } = new();
        public PlaybackSettings PlaybackSettings { get; set; } = new();
    }

    public class TextSettings
    {
        public bool ServiceOn { get; set; } = false;
        public string ApiKey { get; set; } = "";
        public string Name { get; set; } = "Ashton Esquivel";
        public string Phone { get; set; } = "10digitphonenumber";
    }

    public class PlaybackSettings
    {
        public bool Enabled { get; set; } = false;
        public int SaveInterval { get; set; } = 1; // Save every frame (2 = every other frame)
    }
}

using Serilog;
using System.Runtime.InteropServices;
using System.Text.Json;
using VitaWave.ModuleControl.Interfaces;

namespace VitaWave.ModuleControl.Settings
{
    internal static class SettingsManager
    {
        private const string _fileName = "settings.json";
        private const string _folder = "vitawave";

        private static string _filePath;

        static SettingsManager()
        {
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                var winPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), _folder);
                if (!Directory.Exists(winPath))
                    Directory.CreateDirectory(winPath);

                _filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), _folder, _fileName);
            }
            else
            {
                // TODO linux. Somewhere simple
                _filePath = "";
                //Path.Combine("/home", username, "Documents");
            }
        }

        public static Config? GetSettings()
        {
            Config? settings = null;


            if (!File.Exists(_filePath))
            {
                settings = Config.Default;
                SaveSettings(settings);
                return settings;
            }

            try
            {
                var json = File.ReadAllText(_fileName);
                settings = JsonSerializer.Deserialize<Config>(json) ?? throw new Exception();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error getting runtime settings");
            }

            return settings;
        }

        public static void SaveSettings(Config settings)
        {
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_filePath, json);
        }
    }
}

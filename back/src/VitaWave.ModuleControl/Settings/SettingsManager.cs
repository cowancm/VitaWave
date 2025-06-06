using Serilog;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using VitaWave.ModuleControl.Interfaces;

namespace VitaWave.ModuleControl.Settings
{
    internal static class SettingsManager
    {
        private const string _settingsFileName = "settings.json";
        private const string _folder = "vitawave";

        private static string _filePath;

        static SettingsManager()
        {
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                var winPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), _folder);
                if (!Directory.Exists(winPath))
                    Directory.CreateDirectory(winPath);

                _filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), _folder);
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


            if (!File.Exists(Path.Combine(_filePath, _settingsFileName)))
            {
                settings = Config.Default;
                SaveSettings(settings);
                return settings;
            }

            try
            {
                var json = File.ReadAllText(Path.Combine(_filePath, _settingsFileName));
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
            File.WriteAllText(Path.Combine(_filePath, _settingsFileName), json);
        }

        public static string GetModuleConfigPath()
        {
            return Directory.GetFiles(Path.Combine(_filePath), "*.cfg").First();
        }
    }
}

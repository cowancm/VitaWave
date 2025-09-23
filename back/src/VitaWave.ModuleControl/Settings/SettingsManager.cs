using Serilog;
using System.Text.Json;

namespace VitaWave.ModuleControl.Settings
{
    internal static class SettingsManager
    {
        private const string _folder = "~/vitawave";
        private const string _configFileName = "settings.json";
        private const string _TISettingsGlob = "*.cfg";

        private static string _configSettingsPath;

        static SettingsManager()
        {
            if (!Directory.Exists(_folder))
                Directory.CreateDirectory(_folder);

            _configSettingsPath = Path.Combine(_folder, _configFileName);

            if (!File.Exists(_configSettingsPath))
            {
                var defaultConfig = Config.Default;
                if (!OperatingSystem.IsWindows())
                {
                    defaultConfig.DataPortName = "/dev/ttyUSB0";
                    defaultConfig.CliPortName = "/dev/ttyUSB1"; //maybe needs to be flipped?
                }
                
                SaveSettings(Config.Default, _configSettingsPath);
            }
        }

        public static Config GetConfigSettings()
        {
            try
            {
                var json = File.ReadAllText(_configSettingsPath);
                return JsonSerializer.Deserialize<Config>(json) ?? throw new Exception();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error getting runtime settings");
                return Config.Default;
            }
        }

        public static void SaveSettings(object settings, string path)
        {
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);
        }

        public static string GetTIConfigPath()
        {
            return Directory.GetFiles(_folder, _TISettingsGlob).First();
        }
    }
}

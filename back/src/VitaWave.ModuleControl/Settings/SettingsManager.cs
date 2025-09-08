using Serilog;
using System.Text.Json;

namespace VitaWave.ModuleControl.Settings
{
    internal static class SettingsManager
    {
        private const string _configFileName = "settings.json";
        private const string _connectionFileName = "connection.json";
        private const string _folder = "vitawave";
        private const string _TISettingsGlob = "*.cfg";

        private static string _configSettingsPath;
        private static string _connectionPath;

        static SettingsManager()
        {
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), _folder);
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            _configSettingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), _folder, _configFileName);
            _connectionPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), _folder, _connectionFileName);

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

            if (!File.Exists(_connectionPath))
            {
                SaveSettings(APIConnection.Default, _connectionPath);
            }
        }

        public static Config? GetConfigSettings()
        {
            try
            {
                var json = File.ReadAllText(_configSettingsPath);
                return JsonSerializer.Deserialize<Config>(json) ?? throw new Exception();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error getting runtime settings");
            }

            return null;
        }

        public static APIConnection GetNetworkSettings()
        {
            try
            {
                var json = File.ReadAllText(_connectionPath);
                return JsonSerializer.Deserialize<APIConnection>(json) ?? throw new Exception();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error getting connection");
                SaveSettings(APIConnection.Default, _connectionPath);
                return APIConnection.Default;
            }
        }

        public static void SaveSettings(object settings, string path)
        {
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);
        }

        public static string GetTIConfigPath()
        {
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), _folder);
            return Directory.GetFiles(Path.Combine(path, _TISettingsGlob)).First();
        }
    }
}

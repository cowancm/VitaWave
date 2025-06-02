using Serilog;
using System.Text.Json;
using VitaWave.ModuleControl.Interfaces;

namespace VitaWave.ModuleControl.Settings
{
    internal class RuntimeSettingsManager : IRuntimeSettingsManager
    {

        private string _fileName = "config.json";

        public Config? GetSettings()
        {
            Config? settings = null;


            if (!File.Exists(_fileName))
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

        public void SaveSettings(Config settings)
        {
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_fileName, json);
        }
    }
}

using Serilog;
using System.Text.Json;
using VitaWave.ModuleControl.Interfaces;

namespace VitaWave.ModuleControl.Settings
{
    internal class RuntimeSettingsManager : IRuntimeSettingsManager
    {
        private const string ConfigPath = "runtimesettings.json";

        public RuntimeSettings? GetSettings()
        {
            var json = File.ReadAllText(ConfigPath);

            RuntimeSettings? settings = null;

            try
            {
                settings = JsonSerializer.Deserialize<RuntimeSettings>(json) ?? throw new Exception();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error getting runtime settings");
            }

            return settings;
        }

        public void SaveSettings(RuntimeSettings settings)
        {
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ConfigPath, json);
        }
    }
}

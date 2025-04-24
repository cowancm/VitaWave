using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using VitaWave.ModuleControl.Interfaces;

namespace VitaWave.ModuleControl.Settings
{
    internal class RunTimeSettingsManager : IRuntimeSettingsManager
    {
        private const string ConfigPath = "runtimesettings.json";

        public RuntimeSettings? GetSettings()
        {
            if (!File.Exists(ConfigPath))
                return RuntimeSettings.Default;

            var json = File.ReadAllText(ConfigPath);
            return JsonSerializer.Deserialize<RuntimeSettings>(json);
        }

        public void SaveSettings(RuntimeSettings settings)
        {
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ConfigPath, json);
        }
    }
}

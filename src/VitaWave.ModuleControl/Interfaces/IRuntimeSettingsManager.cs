using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using VitaWave.ModuleControl.Settings;

namespace VitaWave.ModuleControl.Interfaces
{
    public interface IRuntimeSettingsManager
    {
        RuntimeSettings? GetSettings();
        void SaveSettings(RuntimeSettings settings);
    }
}

using VitaWave.ModuleControl.Settings;

namespace VitaWave.ModuleControl.Interfaces
{
    public interface IRuntimeSettingsManager
    {
        RuntimeSettings? GetSettings();
        void SaveSettings(RuntimeSettings settings);
    }
}

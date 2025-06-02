using VitaWave.ModuleControl.Settings;

namespace VitaWave.ModuleControl.Interfaces
{
    public interface IRuntimeSettingsManager
    {
        Config? GetSettings();
        void SaveSettings(Config settings);
    }
}

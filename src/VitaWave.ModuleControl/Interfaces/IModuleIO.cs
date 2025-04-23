namespace VitaWave.Common.Interfaces
{
    public interface IModuleIO
    {
        public event EventHandler? OnConnectionLost;
        public bool TryInitializePorts(string dataPortName, string cliPortName);
        public bool TryStartDataPolling();
        public void Stop();
        public bool TryWriteConfigFromFile();
        public bool TryWriteConfigFromFile(string[] content);
    }
}

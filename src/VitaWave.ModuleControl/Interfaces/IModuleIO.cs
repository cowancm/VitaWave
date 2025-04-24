namespace VitaWave.Common.Interfaces
{
    public interface IModuleIO
    {
        public event EventHandler? OnConnectionLost;
        public bool TryInitializePorts(string dataPortName, string cliPortName);
        public bool TryStartDataPolling();
        public void Close();
        public bool TryWriteConfigFromFile();
        public bool TryWriteConfigFromFile(string[] content);
    }
}

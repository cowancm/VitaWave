using ModuleControl.Communication;

namespace Common.Interfaces
{
    public interface IModuleIO
    {
        public event EventHandler<FrameEventArgs>? OnFrameProcessed;
        public event EventHandler? OnConnectionLost;
        public bool TryInitializePorts(string dataPortName, string cliPortName);
        public bool TryStartDataPolling();
        public void Stop();
        public bool TryWriteConfig();
    }
}

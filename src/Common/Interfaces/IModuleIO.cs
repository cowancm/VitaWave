using ModuleControl.Communication;

namespace Common.Interfaces
{
    public interface IModuleIO
    {
        public event EventHandler<FrameEventArgs>? OnFrameProcessed;
        public event EventHandler? OnConnectionLost;
        public void InitializePorts(string dataPortName, string cliPortName);
        public bool StartDataPolling();
        public void Stop();
        public bool TryWriteConfig();
    }
}

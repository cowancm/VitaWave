using ModuleControl.Communication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModuleControl.Interfaces
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

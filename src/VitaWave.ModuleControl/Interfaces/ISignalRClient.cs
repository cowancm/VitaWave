
using System.Data.Common;

namespace VitaWave.ModuleControl.Interfaces
{
    public interface ISignalRClient
    {
        public Task StartAsync();
        public Task SendDataAsync(object obj);
        public Task OnRecieveFromServer(string data);
    }
}

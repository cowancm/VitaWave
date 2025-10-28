
using Microsoft.AspNetCore.SignalR.Client;

namespace VitaWave.ModuleControl.Interfaces
{
    public interface ISignalRClient
    {
        HubConnectionState Status { get; }
        public Task StartAsync();
        public Task SendDataAsync(object data);
        public void SubscribeToModuleStatus(IModuleIO io);
    }
}

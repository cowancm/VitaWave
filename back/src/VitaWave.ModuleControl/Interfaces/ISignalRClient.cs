
using Microsoft.AspNetCore.SignalR.Client;

namespace VitaWave.ModuleControl.Interfaces
{
    public interface ISignalRClient
    {
        HubConnectionState Status { get; }
        public Task StartAsync();
        public Task SendDataAsync(object data);
        public Task ModuleConnectionRequest();
        public void SubscribeToModuleStatus(IModuleIO io);
    }
}

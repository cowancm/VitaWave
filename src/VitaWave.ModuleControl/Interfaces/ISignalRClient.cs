
namespace VitaWave.ModuleControl.Interfaces
{
    public interface ISignalRClient
    {
        public Task StartAsync();
        public Task SendDataAsync(object data);
        public Task ModuleConnectionRequest();
        public void SubscribeToModuleStatus(IModuleIO io);
    }
}

using Microsoft.AspNetCore.SignalR;
using VitaWave.Common.ModuleToAPI;

namespace VitaWave.WebAPI.Hubs
{
    public class ModuleHub : Hub
    {
        private readonly DataFacilitator _dataFacilitator;

        public ModuleHub(DataFacilitator dataFacilitator) 
        {
            _dataFacilitator = dataFacilitator;
        }

        public async Task RecieveModuleData(string data)
        {
            await Clients.All.SendAsync("Module", $"Message Recieved from the server!:\n {data}");
        }

        public async Task OnRecieveModuleData(EventPacket dataPacket)
        {
            await _dataFacilitator.OnNewData(dataPacket);
        }

        public async Task OnReceiveModuleStatus(string status)
        {
            //TODO
        }
    }
}

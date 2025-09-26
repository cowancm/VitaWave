using Microsoft.AspNetCore.SignalR;
using VitaWave.Common;
using VitaWave.Data;

namespace VitaWave.WebAPI.Hubs
{
    public class ModuleHub : Hub
    {
        public readonly DataFacilitator dataFacilitator;
        public ModuleHub(DataFacilitator dataFacilitator)
        {
            this.dataFacilitator = dataFacilitator;
        }

        public async Task ModuleData(EventPacket dataPacket)
        {
            dataFacilitator.Add(dataPacket);
        }
    }
}
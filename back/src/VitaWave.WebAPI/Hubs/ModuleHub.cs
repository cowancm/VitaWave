using Microsoft.AspNetCore.SignalR;
using VitaWave.Common;
using VitaWave.Data;

namespace VitaWave.WebAPI.Hubs
{
    public class ModuleHub : Hub
    {
        public readonly DataFacilitator dataFacilitator;
        public event EventHandler<object> Disconnected;
        public ModuleHub(DataFacilitator dataFacilitator)
        {
            this.dataFacilitator = dataFacilitator;
        }

        public async Task ModuleData(EventPacket dataPacket)
        {
            dataFacilitator.Add(dataPacket);
        }

        public async Task ModuleIdentifier(string identifier)
        {
            ModuleHubState.Add(Context.ConnectionId, identifier);
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            var moduleID = ModuleHubState.Remove(Context.ConnectionId);

            if (moduleID != null)
            {
                dataFacilitator.Clear(moduleID); //should clear alg buffer
            }

            return base.OnDisconnectedAsync(exception);
        }
    }
}
using Microsoft.AspNetCore.SignalR;
using Serilog;
using VitaWave.Common;
using VitaWave.Data;

namespace VitaWave.WebAPI.Hubs
{
    public class ModuleHub : Hub
    {
        public readonly DataFacilitator _dataFacilitator;
        public readonly IHubContext<ChartHub> _chartHub;
        public event EventHandler<object> Disconnected;

        public ModuleHub(DataFacilitator dataFacilitator, IHubContext<ChartHub> chartHub)
        {
            _dataFacilitator = dataFacilitator;
            _chartHub = chartHub;
        }

        public async Task ModuleData(EventPacket dataPacket)
        {
            _dataFacilitator.Add(dataPacket);
            await _chartHub.BroadcastUnfilteredPoints(dataPacket.ToPersonPointSet());
        }

        public async Task ModuleIdentifier(string identifier)
        {
            ModuleHubState.Add(Context.ConnectionId, identifier);
            Log.Information($"Connected to module: {identifier}");
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            var moduleID = ModuleHubState.Remove(Context.ConnectionId);

            if (moduleID != null)
            {
                _dataFacilitator.Clear(moduleID); //should clear alg buffer
            }

            return base.OnDisconnectedAsync(exception);
        }
    }
}
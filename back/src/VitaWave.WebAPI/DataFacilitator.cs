using Microsoft.AspNetCore.SignalR;
using VitaWave.WebAPI.Hubs;
using VitaWave.Data;
using Serilog;
using VitaWave.Common.APIToWebserver;
using VitaWave.Common;

namespace VitaWave.WebAPI
{
    public class DataFacilitator
    {
        private readonly IHubContext<ModuleHub> _moduleHub;
        private readonly IHubContext<WebHub> _webHub;

        public DataFacilitator(IHubContext<ModuleHub> moduleHub, IHubContext<WebHub> webHub)
        {
            _moduleHub = moduleHub;
            _webHub = webHub;
        }

        const string RawVisualizerMethodName = "OnUnfilteredPoints";
        const string FilteredVisualizerMethodName = "OnFilteredPoints";
        public async Task OnNewData(EventPacket eventPacket)
        {
            var visualizerPoints = eventPacket.ToPersonPointSet();

            var jsonString = System.Text.Json.JsonSerializer.Serialize(visualizerPoints);
            Log.Information($"Sending: {jsonString}");

            await _webHub.Clients.All.SendAsync(RawVisualizerMethodName, visualizerPoints);
            
            //TODO
            //aggregate data somehow, prolly some type of IDataAggregor that takes this eventpacket
            //either await response from the aggregator, or fire and forget and the aggregator calls on
            //the data facilitator when there is an update. maybe will need a queue system...?
        }

    }
}

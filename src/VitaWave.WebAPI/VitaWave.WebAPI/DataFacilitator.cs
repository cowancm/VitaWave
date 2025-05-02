using Microsoft.AspNetCore.SignalR;
using VitaWave.Common.ModuleToAPI;
using VitaWave.WebAPI.Hubs;
using VitaWave.Data;

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
            await _webHub.Clients.All.SendAsync(RawVisualizerMethodName, visualizerPoints);
            
            //TODO
            //await _webHub.Clients.All.SendAsync(FilteredVisualizerMethodName, foo);
            //most likely will be a bit diff than this though
        }

    }
}

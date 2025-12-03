using Microsoft.AspNetCore.SignalR;
using Serilog;
using VitaWave.Common;
using VitaWave.Data;

namespace VitaWave.WebAPI.Hubs
{
    public class ModuleHub : Hub
    {
        public readonly IHubContext<ChartHub> _chartHub;
        private readonly DataFacilitator _dataFacilitator;

        public ModuleHub(IHubContext<ChartHub> chartHub, DataFacilitator dataFacilitator)
        {
            _chartHub = chartHub;
            _dataFacilitator = dataFacilitator;
            ChartHubSends.hubContext = chartHub;
        }

        public static ResultEvent lastEvent { get; set; } = new()
        {
            Target = new()
        };

        public async Task ModuleData(List<ResultEvent> results)
        {
            foreach (var resultEvent in results)
            if (resultEvent.ResultId != lastEvent.ResultId) // bandaid for now
            {
                _dataFacilitator.Add(resultEvent);
            }

            var points = results.Select(e => e.Target);
            if (points.Any())
                await _chartHub.BroadcastUnfilteredPoints(points);

            lastEvent = results.LastOrDefault(new ResultEvent() { Target = new() });
        }

        public async Task ModuleIdentifier(string identifier)
        {
            ModuleHubState.Add(Context.ConnectionId, identifier);
            Log.Information($"Connected to module: {identifier}");
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            var moduleID = ModuleHubState.Remove(Context.ConnectionId);
            return base.OnDisconnectedAsync(exception);
        }
    }
}
using Microsoft.Extensions.Hosting;
using VitaWave.Common.Interfaces;
using VitaWave.ModuleControl.Interfaces;

namespace VitaWave.ModuleControl.Services
{
    internal class ModuleService : BackgroundService
    {
        private readonly IModuleIO _moduleIO;
        private readonly IDataAggregator _aggregator;
        private readonly ISignalRClient _signalRClient;

        public ModuleService(IModuleIO moduleIO, IDataAggregator aggregator, ISignalRClient client)
        {
            _moduleIO = moduleIO;
            _aggregator = aggregator;
            _signalRClient = client;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
    }
}

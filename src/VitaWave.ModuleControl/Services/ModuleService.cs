using Microsoft.Extensions.Hosting;
using VitaWave.Common.Interfaces;

namespace VitaWave.ModuleControl.Services
{
    internal class ModuleService : BackgroundService
    {
        private readonly IModuleIO _moduleIO;

        public ModuleService(IModuleIO moduleIO)
        {
            _moduleIO = moduleIO;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }

    }
}

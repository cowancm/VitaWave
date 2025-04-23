using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using VitaWave.Common.Interfaces;
using VitaWave.ModuleControl.Client;
using VitaWave.ModuleControl.DataAggregation;
using VitaWave.ModuleControl.Interfaces;
using VitaWave.ModuleControl.Parsing;
using VitaWave.ModuleControl.Services;

namespace ModuleControl
{
    class Program
    {
        static void Main(string[] args)
        {
            var builder = Host.CreateApplicationBuilder();

            builder.Services.AddSingleton<IModuleIO, ModuleIO>();
            builder.Services.AddSingleton<IDataAggregator, DataAggregator>();
            builder.Services.AddSingleton<ISignalRClient, SignalRClient>();
            builder.Services.AddHostedService<ModuleService>();

            var host = builder.Build();
            host.RunAsync();
        }
    }
}
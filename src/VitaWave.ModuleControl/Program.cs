using Serilog;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using VitaWave.Common.Interfaces;
using VitaWave.ModuleControl;
using VitaWave.ModuleControl.Client;
using VitaWave.ModuleControl.DataAggregation;
using VitaWave.ModuleControl.Interfaces;
using VitaWave.ModuleControl.Parsing;

namespace ModuleControl
{
    class Program
    {
        static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .CreateLogger();

            var builder = Host.CreateApplicationBuilder();

            builder.Services.AddSingleton<IModuleIO, ModuleIO>()
                            .AddSingleton<IDataAggregator, DataAggregator>()
                            .AddSingleton<ISignalRClient, SignalRClient>()
                            .AddHostedService<ModuleService>();

            var host = builder.Build();
            host.RunAsync();
        }
    }
}
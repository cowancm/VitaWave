using Serilog;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using VitaWave.ModuleControl;
using VitaWave.ModuleControl.Client;
using VitaWave.ModuleControl.DataAggregation;
using VitaWave.ModuleControl.Interfaces;
using VitaWave.ModuleControl.Parsing;
using VitaWave.ModuleControl.Settings;
using Microsoft.Extensions.Configuration;

namespace ModuleControl
{
    class Program
    {
        static async Task Main(string[] args)
        {
            try
            {
                Log.Logger = new LoggerConfiguration()
                    .MinimumLevel.Information()
                    .WriteTo.Console()
                    .CreateLogger();

                await Host.CreateDefaultBuilder(args)
                    .ConfigureAppConfiguration((context, config) =>
                    {
                        //lata
                        //config.AddJsonFile("appsettings.json", optional: false)
                        //      .AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: true)
                        //      .AddEnvironmentVariables();
                    })
                    .ConfigureServices((context, services) =>
                    {
                        services.AddSingleton<IRuntimeSettingsManager, RuntimeSettingsManager>()
                                .AddSingleton<ISerialProcessor, SerialDataProcessor>()
                                .AddSingleton<IModuleIO, ModuleIO>()
                                .AddSingleton<IDataAggregator, DataAggregator>()
                                .AddSingleton<ISignalRClient, SignalRClient>()
                                .AddHostedService<ModuleService>();
                    })
                    .RunConsoleAsync();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Host terminated unexpectedly");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
    }
}
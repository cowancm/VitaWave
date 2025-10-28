using Serilog;
using VitaWave.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using AlgTesting;

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
                    .ConfigureServices((context, services) =>
                    {
                        services.AddSingleton<DataProcessor>();
                        services.AddSingleton<DataFacilitator>();
                        services.AddSingleton<Tester>();
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
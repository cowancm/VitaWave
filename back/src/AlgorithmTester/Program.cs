using Serilog;
using VitaWave.ModuleControl.Interfaces;
using VitaWave.ModuleControl.Data;
using AlgorithmTester;
using System.Text.Json.Serialization;
using System.Text.Json;
using VitaWave.Common;

namespace ModuleControl
{
    class Program
    {
        static int Main(string[] args)
        {
            try
            {
                Log.Logger = new LoggerConfiguration()
                    .MinimumLevel.Information()
                    .WriteTo.Console()
                    .CreateLogger();

                var signalR = new DoNothingSignalRClient();
                var moduleTracker = new ModuleTargetTracker(signalR);

                var filePath = args[0];
                var json = File.ReadAllText(filePath);
                var events = JsonSerializer.Deserialize<List<EventPacket>>(json);

            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Host terminated unexpectedly");
            }
            finally
            {
                Log.CloseAndFlush();
            }
            return 0;

        }
    }
}
using ModuleControl.Communication;
using Common.Interfaces;
using ModuleControl.Utils;
using System.Reflection;

namespace ModuleControl
{
    class Program
    {
        static ModuleIO? _moduleIO;
        static (string, string) COMS;

        static void Main(string[] args)
        {
            ConsoleHelpers.OutputFancyLabel();
            
            var doWeDataLog = ConsoleHelpers.AskAboutDataLogging();

            _moduleIO = ModuleIO.Instance;
            COMS = ConsoleHelpers.GetComPorts();
            StartConnection();

            _moduleIO.OnFrameProcessed += PrintFrame;

            while(Console.ReadKey().Key != ConsoleKey.Spacebar)
            {
                Thread.Sleep(200);
            }
        }

        static void PrintFrame(object? sender, FrameEventArgs e)
        {
            //Console.WriteLine($"Frame Recieved with points: {e.FrameEvent?.Points?.Count.ToString() ?? "No Points"}");
            ConsoleHelpers.PrintTargetIndication(e.FrameEvent);
        }

        static void StartConnection()
        {
            _moduleIO?.InitializePorts(COMS.Item1, COMS.Item2);
            _moduleIO?.TryWriteConfig();
            _moduleIO?.StartDataPolling();
        }
    }
}
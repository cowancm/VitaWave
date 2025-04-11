using ModuleControl.Communication;
using Common.Interfaces;
using ModuleControl.Utils;
using System.Reflection;

namespace ModuleControl
{

    //used purely for testing purposes...
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
            _moduleIO.OnConnectionLost += OnConnectionLost;

            while(Console.ReadKey().Key != ConsoleKey.Spacebar)
            {
                Thread.Sleep(200);
            }
        }
        static void OnConnectionLost(object? sender, EventArgs e)
        {
            Console.WriteLine("Lost connection");

            Console.WriteLine("Please press the enter key to begin connection again.");

            while(Console.ReadKey().Key != ConsoleKey.Enter)
            {
                Thread.Sleep(200);
            }

            try
            {
                StartConnection();
            }
            catch (Exception ex)
            {
                OnConnectionLost(null, null);
            }
        }

        static void PrintFrame(object? sender, FrameEventArgs e)
        {
            Console.WriteLine($"Frame Recieved with points: {e.FrameEvent?.Points?.Count.ToString() ?? "No Points"}");
        }

        static void StartConnection()
        {
            _moduleIO?.InitializePorts(COMS.Item1, COMS.Item2);
            _moduleIO?.TryWriteConfig();
            _moduleIO?.StartDataPolling();
        }
    }
}
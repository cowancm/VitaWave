using ModuleControl.Communication;
using Common.Interfaces;
using ModuleControl.Utils;

namespace ModuleControl
{

    //used purely for testing purposes...
    class Program
    {
        static void Main(string[] args)
        {
            ConsoleHelpers.OutputFancyLabel();
            
            var doWeDataLog = ConsoleHelpers.AskAboutDataLogging();

            var moduleIO = ModuleIO.Instance;
            var coms = ConsoleHelpers.GetComPorts();

            moduleIO.InitializePorts(coms.Item1, coms.Item2);
            moduleIO.TryWriteConfig();
            moduleIO.StartDataPolling();

            moduleIO.OnFrameProcessed += PrintFrame;
        }
        static void OnConnectionLost(object? sender, EventArgs e)
        {
            
        }

        static void PrintFrame(object? sender, FrameEventArgs e)
        {
            Console.WriteLine($"Frame Recieved with points: {e.FrameEvent?.Points?.Count.ToString() ?? "No Points"}");
        }
    }
}
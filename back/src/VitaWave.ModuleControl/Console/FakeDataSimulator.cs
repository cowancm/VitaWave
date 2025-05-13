using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VitaWave.ModuleControl.Console;
using VitaWave.ModuleControl.Interfaces;
using VitaWave.ModuleControl.Simulating;

internal static class FakeDataSimulator
{
    static bool stop;

    public static async Task RunFakeDataSimLoop(CancellationToken token, FakeDataPusher pusher)
    {
        Console.WriteLine("Console controller started. Type 'help' for commands.");
        stop = false;

        if (pusher is null)
            return;

        try
        {
            while (!token.IsCancellationRequested && !stop)
            {
                if (Console.KeyAvailable)
                {
                    var input = Console.ReadLine();
                    await ProcessFakeDataCommand(input ?? "", token, pusher);
                }
                else
                {
                    await Task.Delay(50, token);
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in fake data simulator: {ex.Message}");
        }

        Console.WriteLine("Exiting fake data simulator");
    }

    public static async Task ProcessFakeDataCommand(string input, CancellationToken token, FakeDataPusher pusher)
    {
        if (string.IsNullOrWhiteSpace(input)) return;

        switch (input.ToLower())
        {
            case "exit":
                stop = true;    
                break;
            default:
                Console.WriteLine($"Sending default");
                await pusher.PushData(FakeData.CreateSingle());
                break;
        }
    }
}

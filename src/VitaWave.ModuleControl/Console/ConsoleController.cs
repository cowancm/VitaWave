using System;
using System.Threading;
using System.Threading.Tasks;
using VitaWave.ModuleControl.Interfaces;

public class ConsoleController
{
    //i had AI write this stub. will implement my own shizaz soon

    private readonly CancellationTokenSource _cts = new CancellationTokenSource();
    private Task? _consoleTask;

    private readonly IModuleIO _moduleIO;

    public ConsoleController(IModuleIO moduleIO)
    {
        _moduleIO = moduleIO;
    }

    public void Start()
    {
        if (_consoleTask != null) return;

        _consoleTask = Task.Run(() => RunConsoleLoop(_cts.Token));
    }

    public void Stop()
    {
        _cts.Cancel();
        _consoleTask?.Wait();
        _consoleTask = null;
    }

    private async Task RunConsoleLoop(CancellationToken token)
    {
        Console.WriteLine("Console controller started. Type 'help' for commands.");

        try
        {
            while (!token.IsCancellationRequested)
            {
                if (Console.KeyAvailable)
                {
                    var input = Console.ReadLine();
                    await ProcessCommand(input, token);
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
            Console.WriteLine($"Error in console controller: {ex.Message}");
        }

        Console.WriteLine("Console controller stopped.");
    }


    //AI generated.Implementation pending atm
    private async Task ProcessCommand(string? command, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(command)) return;

        switch (command.ToLower())
        {
            case "help":
                Console.WriteLine("Available commands:");
                Console.WriteLine("  help - Show this help");
                Console.WriteLine("  start - Start the module");
                Console.WriteLine("  stop - Stop the module");
                Console.WriteLine("  status - Show current status");
                Console.WriteLine("  exit - Exit the application");
                break;

            case "start":
                var status = _moduleIO.Run();
                Console.WriteLine($"Module started. Status: {status}");
                break;

            case "stop":
                status = _moduleIO.Stop();
                Console.WriteLine($"Module stopped. Status: {status}");
                break;

            case "status":
                Console.WriteLine($"Current status: {_moduleIO.Status}");
                break;

            case "config":
                await _moduleIO.TryWriteConfigFromFile();
                Console.WriteLine("Configuration written to module.");
                break;

            case "exit":
                // Signal the application to exit
                Environment.Exit(0);
                break;

            default:
                Console.WriteLine($"Unknown command: {command}");
                break;
        }
    }
}
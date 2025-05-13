using System;
using System.Threading;
using System.Threading.Tasks;
using VitaWave.ModuleControl.Console;
using VitaWave.ModuleControl.Interfaces;
using VitaWave.ModuleControl.Simulating;

public class ConsoleController
{
    private readonly CancellationTokenSource _cts = new CancellationTokenSource();
    private Task? _consoleTask;
    private CancellationToken ct;

    private readonly IModuleIO _moduleIO;
    private readonly ISignalRClient _signalRClient;

#if DEBUG
    private readonly FakeDataPusher _fakeDataPusher;
#endif

    public ConsoleController(IModuleIO moduleIO, ISignalRClient client)
    {
        _moduleIO = moduleIO;
        _signalRClient = client;

#if DEBUG
        _fakeDataPusher = new(client);
#endif
    }

    public void Start()
    {
        if (_consoleTask != null) return;

        ConsoleHelpers.OutputFancyLabel();

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


    private async Task ProcessCommand(string? command, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(command)) return;

        switch (command.ToLower())
        {
            case "help":
                Console.WriteLine("Available commands:");
                Console.WriteLine("  init     - initialize ports");
                Console.WriteLine("  config  - send config file to module");
                Console.WriteLine("  run     - run the parser");
                Console.WriteLine("  stop    - stop and close the parser");
                Console.WriteLine("  pause   - pause the parser");
                Console.WriteLine("  exit    - Exit the application");
                break;

            case "init":
                var status = _moduleIO.InitializePorts();
                Console.WriteLine($"Module started. Status: {status}");
                break;

            case "run":
                status = _moduleIO.Run();
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
                var result = (await _moduleIO.TryWriteConfigFromFile()) ? "Success" : "Failed";
                Console.WriteLine($"Configuration written to module. Result: {result}");
                break;

            case "pause":
                status = _moduleIO.Pause();
                Console.WriteLine($"Module attempted to be paused. \nStatus: {status}");
                break;
#if DEBUG
            case "fake":
                await ConsoleFakeDataSimulator.RunFakeDataSimLoop(token, _fakeDataPusher);
                break;
#endif

            case "exit":
                Environment.Exit(0);
                break;

            default:
                Console.WriteLine($"Unknown command: {command}");
                break;
        }
    }
}
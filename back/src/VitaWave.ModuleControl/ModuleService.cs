using Microsoft.Extensions.Hosting;
using VitaWave.ModuleControl.Client;
using VitaWave.ModuleControl.Interfaces;

internal class ModuleService : BackgroundService
{
    private readonly ISignalRClient _signalRClient;
    private readonly IModuleIO _moduleIO;
    private readonly ISerialProcessor _serialProcessor;
    private readonly ConsoleController _consoleController;

    public ModuleService(
        ISignalRClient client,
        ISerialProcessor serialProcessor,
        IModuleIO moduleIO)
    {
        _signalRClient = client;
        _serialProcessor = serialProcessor;
        _moduleIO = moduleIO;
#if DEBUG
        _consoleController = new ConsoleController(moduleIO, client);
#endif
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
#if DEBUG
            _consoleController.Start();
#endif
            _serialProcessor.Run();
            await _signalRClient.StartAsync();
            _signalRClient.SubscribeToModuleStatus(_moduleIO);
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            _consoleController.Stop();
            _moduleIO.Stop();
        }
    }
}
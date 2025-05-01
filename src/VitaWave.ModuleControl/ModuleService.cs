using Microsoft.Extensions.Hosting;
using VitaWave.ModuleControl.Client;
using VitaWave.ModuleControl.Interfaces;

internal class ModuleService : BackgroundService
{
    private readonly IDataAggregator _aggregator;
    private readonly ISignalRClient _signalRClient;
    private readonly IModuleIO _moduleIO;
    private readonly ISerialProcessor _serialProcessor;
    private readonly ConsoleController _consoleController;

    private readonly ModuleClient moduleClient;

    public ModuleService(
        IDataAggregator aggregator,
        ISignalRClient client,
        ISerialProcessor serialProcessor,
        IModuleIO moduleIO)
    {
        _aggregator = aggregator;
        _signalRClient = client;
        _serialProcessor = serialProcessor;
        _moduleIO = moduleIO;
        _consoleController = new ConsoleController(moduleIO, client);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _consoleController.Start();
            _serialProcessor.Run();

            await _signalRClient.StartAsync();
            var status = _moduleIO.InitializePorts();

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
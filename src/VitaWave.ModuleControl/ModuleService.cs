using Microsoft.Extensions.Hosting;
using VitaWave.ModuleControl.Interfaces;

internal class ModuleService : BackgroundService
{
    private readonly IDataAggregator _aggregator;
    private readonly ISignalRClient _signalRClient;
    private readonly IModuleIO _moduleIO;
    private readonly ConsoleController _consoleController;

    public ModuleService(
        IDataAggregator aggregator,
        ISignalRClient client,
        IModuleIO moduleIO)
    {
        _aggregator = aggregator;
        _signalRClient = client;
        _moduleIO = moduleIO;
        _consoleController = new ConsoleController(moduleIO);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _consoleController.Start();

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
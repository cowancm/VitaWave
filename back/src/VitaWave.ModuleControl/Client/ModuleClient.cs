using Microsoft.AspNetCore.SignalR.Client;
using Serilog;
using VitaWave.Common.ModuleToAPI;
using VitaWave.ModuleControl.Interfaces;
using VitaWave.ModuleControl.Parsing;

namespace VitaWave.ModuleControl.Client
{
    internal class ModuleClient : ISignalRClient
    {
        public HubConnectionState Status => _connection.State;
        private HubConnection _connection;
        private IModuleIO? _IO;
        const string serverURL = "http://localhost:5278/module"; //this is going in a settings file at some point.
        const int MaxInitialConnectAttempts = 5;

        public ModuleClient()
        {
            _connection = new HubConnectionBuilder()
                .WithUrl(serverURL)
                .WithAutomaticReconnect() //0s, 2s, 10s, 30s
                .Build();

            _connection.Reconnecting += (error) =>
            {
                Log.Warning("Server reconnecting: {Error}", error?.Message);
                return Task.CompletedTask;
            };

            _connection.Reconnected += (connectionId) =>
            {
                Log.Information("Server reconnected with connectionId: {ConnectionId}", connectionId);
                return Task.CompletedTask;
            };

            _connection.On("OnModuleConnectionRequest", async () =>
            {
                await ModuleConnectionRequest();
            });
        }


        public async Task StartAsync()
        {
            int attempt = 0;

            while (_connection.State != HubConnectionState.Connected && attempt < MaxInitialConnectAttempts)
            {
                try
                {
                    attempt++;
                    await _connection.StartAsync();
                    Log.Information("Server connection started");
                }
                catch (Exception ex)
                {
                    Log.Warning($"Server connection attempt {attempt} failed to connect.", ex.Message);
                    await Task.Delay(2000 * attempt);
                }
            }

            if (_connection.State != HubConnectionState.Connected)
            {
                Log.Error($"Failed to establish server connection after {MaxInitialConnectAttempts} attempts.");
            }
        }

        const string SendDataMethodName = "OnRecieveModuleData";
        public async Task SendDataAsync(object data)
        {
            await _connection.SendAsync(SendDataMethodName, data);
        }

        public async Task ModuleConnectionRequest()
        {
            await SendStatusAsync(_IO?.Status.ToString() ?? "Unknown");
        }

        const string SendDataModuleStatusName = "OnReceiveModuleStatus";
        private async Task SendStatusAsync(string status)
        {
            await _connection.SendAsync(SendDataModuleStatusName, status);
        }

        public void SubscribeToModuleStatus(IModuleIO io)
        {
            if (_IO is null)
            {
                _IO = io;
            }

            io.PropertyChanged += async (sender, args) =>
            {
                if (args.PropertyName == nameof(io.Status))
                {
                    var status = io.Status;
                    await SendStatusAsync(status.ToString());
                }
            };
        }
    }
}

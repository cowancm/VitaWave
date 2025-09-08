using Microsoft.AspNetCore.SignalR.Client;
using Serilog;
using System.Net.Mail;
using VitaWave.ModuleControl.Interfaces;
using VitaWave.ModuleControl.Settings;

namespace VitaWave.ModuleControl.Client
{
    internal class ModuleClient : ISignalRClient
    {
        public HubConnectionState Status => _connection.State;
        private HubConnection _connection;
        private IModuleIO? _IO;

        const string serverURL = "http://localhost:5278/module"; //this is going in a settings file at some point.
        const int MaxInitialConnectAttempts = int.MaxValue;

        public ModuleClient()
        {
            _connection = new HubConnectionBuilder()
                .WithUrl(SettingsManager.GetNetworkSettings().API_Url)
                .WithAutomaticReconnect(new ReconnectPolicy()) //reconnects every 2 seconds
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

            _connection.On("ModuleStatusRequest", async() =>
            {
                await SendModuleStatusAsync();
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
                    await SendIdentifier();
                }
                catch (Exception ex)
                {
                    Log.Warning($"Server connection attempt {attempt} failed to connect.", ex.Message);
                    await Task.Delay(5000 * attempt);
                }
            }

            if (_connection.State != HubConnectionState.Connected)
            {
                Log.Error($"Failed to establish server connection after {attempt} attempts.");
            }
        }

        const string SendModuleDataName = "ModuleData";
        public async Task SendDataAsync(object data)
        {
            await _connection.SendAsync(SendModuleDataName, data);
        }

        const string SendModuleStatusName = "ModuleStatus";
        private async Task SendModuleStatusAsync()
        {
            await _connection.SendAsync(SendModuleStatusName, _IO?.Status.ToString() ?? "Unknown");
        }

        const string SendIdentifierMethodName = "ModuleRegistration";
        private async Task SendIdentifier()
        {
            await _connection.SendAsync(SendIdentifierMethodName, SettingsManager.GetConfigSettings()?.Identifier ?? "Unknown");
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
                    await SendModuleStatusAsync();
                }
            };
        }
    }
}

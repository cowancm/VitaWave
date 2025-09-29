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

        const int MaxInitialConnectAttempts = int.MaxValue;

        public ModuleClient()
        {
            if (!OperatingSystem.IsWindows())
            {
                var config = SettingsManager.GetConfigSettings();
                Log.Information("http://" + config.API_Server_IP + ":" + config.Port.ToString() + "/module");
                _connection = new HubConnectionBuilder()
                    .WithUrl("http://" + config.API_Server_IP + ":" + config.Port.ToString() + "/module")
                    .WithAutomaticReconnect(new ReconnectPolicy()) //reconnects every 2 seconds
                    .Build();
            }
            else
            {
                _connection = new HubConnectionBuilder()
                    .WithUrl("http://127.0.0.1:5000" + "/module")
                    .WithAutomaticReconnect(new ReconnectPolicy()) //reconnects every 2 seconds
                    .Build();
            }

            _connection.Reconnecting += (error) =>
            {
                Log.Warning("Server reconnecting: {Error}", error?.Message);
                return Task.CompletedTask;
            };

            _connection.Reconnected += async (connectionId) =>
            {
                Log.Information("Server reconnected with connectionId: {ConnectionId}", connectionId);
                await SendModuleIdentifier();
            };
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
                    await SendModuleIdentifier();
                    Log.Information("Server connection started");
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

        public const string SendModuleDataName = "ModuleData";
        public async Task SendDataAsync(object data)
        {
            await _connection.SendAsync(SendModuleDataName, data);
        }

        public const string SendModuleStatusName = "ModuleIdentifier";
        public async Task SendModuleIdentifier()
        {
            await _connection.SendAsync(SendModuleStatusName, SettingsManager.GetConfigSettings().Identifier ?? "Unknown");
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
                    //await SendModuleStatusAsync();
                }
            };
        }
    }
}

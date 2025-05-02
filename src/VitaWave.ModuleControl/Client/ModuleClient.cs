using Microsoft.AspNetCore.SignalR.Client;
using Serilog;
using VitaWave.ModuleControl.Interfaces;

namespace VitaWave.ModuleControl.Client
{
    internal class ModuleClient : ISignalRClient
    {
        private HubConnection _connection;
        private IModuleIO? _IO;
        const string serverURL = "http://localhost:5278/module"; //this is going in a settings file at some point.

        public ModuleClient()
        {
            _connection = new HubConnectionBuilder()
                .WithUrl(serverURL)
                .Build();

            _connection.On("ModuleConnectionRequest", async () =>
            {
                await ModuleConnectionRequest();
            });
        }


        public async Task StartAsync()
        {
            await _connection.StartAsync();
        }

        public async Task SendDataAsync(object data)
        {
            await _connection.SendAsync("RecieveModuleData", data);
        }

        public async Task ModuleConnectionRequest()
        {
            await SendStatusAsync(_IO?.Status.ToString() ?? "Unknown");
        }

        private async Task SendStatusAsync(string status)
        {
            await _connection.SendAsync("RecieveModuleStatus", status);
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

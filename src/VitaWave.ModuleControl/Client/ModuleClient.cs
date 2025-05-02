using Microsoft.AspNetCore.SignalR.Client;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VitaWave.ModuleControl.Interfaces;

namespace VitaWave.ModuleControl.Client
{
    internal class ModuleClient : ISignalRClient
    {
        private HubConnection _connection;
        private readonly IModuleIO _IO;
        const string serverURL = "http://localhost:5278/moduleHub"; //this is going in a settings file at some point.

        public ModuleClient(IModuleIO io)
        {
            _IO = io;
            _IO.OnModuleStatusChanged += _IO_OnConnectionStatusChanged;

            _connection = new HubConnectionBuilder()
                .WithUrl(serverURL)
                .Build();

            _connection.On<string>("Module", (data) =>
            {
                Log.Information("Data received: " + data);
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

        public async Task OnRecieveFromServer(string data)
        {
        }

        private async void _IO_OnConnectionStatusChanged(object? sender, EventArgs e)
        {
            await _connection.SendAsync("Recie");
        }
    }
}

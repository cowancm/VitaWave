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

        const string serverURL = "http://localhost:5278/moduleHub"; //this is going in a settings file at some point.

        public ModuleClient()
        {
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

        public async Task SendDataAsync(string data)
        {
            await _connection.SendAsync("RecieveModuleData", data);
        }

    }
}

// VitaWave.WebAPI/Tcp/TcpCommandServer.cs
using System.Data.Common;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using VitaWave.Common;

namespace VitaWave.WebAPI.Tcp
{
    public class TcpCommandServer
    {
        private readonly int _port = 5001;
        private TcpListener? _listener;
        private CancellationTokenSource? _cts;

        public TcpCommandServer()
        {
            Start();
        }

        public void Start()
        {
            _cts = new();
            _listener = new TcpListener(IPAddress.Loopback, _port);
            _listener.Start();

            Task.Run(async () =>
            {
                while (!_cts.Token.IsCancellationRequested)
                {
                    var client = await _listener.AcceptTcpClientAsync(_cts.Token);
                    _ = HandleClient(client);
                }
            }, _cts.Token);
        }

        private async Task HandleClient(TcpClient client)
        {
            using var stream = client.GetStream();
            var buffer = new byte[1024];

            while (client.Connected)
            {
                int bytesRead = await stream.ReadAsync(buffer);
                if (bytesRead == 0) break;

                string json = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                var cmd = JsonSerializer.Deserialize<DevCommand>(json);

                if (cmd != null)
                {
                    Execute(cmd);
                }
            }
        }

        private async void Execute(DevCommand cmd)
        {
            switch (cmd.CommandType)
            {
                case DevCommandType.ClearData:
                    // Implement clear data logic
                    break;
                case DevCommandType.ResetModule:
                    // Implement reset module logic
                    break;
                default:
                    break;
            }
        }

        public void Stop()
        {
            _cts?.Cancel();
            _listener?.Stop();
        }
    }
}

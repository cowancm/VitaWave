// VitaWave.DevTool/Services/TcpCommandClient.cs
using System.Data.Common;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using VitaWave.Common;

namespace VitaWave.DevTool.Services
{
    public class TcpCommandClient
    {
        private readonly string _host;
        private readonly int _port;

        public TcpCommandClient(string host = "127.0.0.1", int port = 5001)
        {
            _host = host;
            _port = port;
        }

        public async Task SendCommandAsync(DevCommand cmd)
        {
            using var client = new TcpClient();
            await client.ConnectAsync(_host, _port);
            using var stream = client.GetStream();

            string json = JsonSerializer.Serialize(cmd);
            byte[] data = Encoding.UTF8.GetBytes(json);
            await stream.WriteAsync(data);

            var buffer = new byte[1024];
            int bytes = await stream.ReadAsync(buffer);
            string response = Encoding.UTF8.GetString(buffer, 0, bytes);

            Console.WriteLine($"[Response] {response}");
        }
    }
}

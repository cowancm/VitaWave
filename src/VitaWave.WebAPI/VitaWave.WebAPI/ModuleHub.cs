using Microsoft.AspNetCore.SignalR;

namespace VitaWave.WebAPI
{
    public class ModuleHub : Hub
    {
        public async Task RecieveModuleData(string data)
        {
            await Clients.All.SendAsync("Module", $"Message Recieved from the server!:\n {data}");
        }

        public async Task ReceiveWebpageRequest(string message)
        {
            Console.WriteLine($"[Server] Received message from client: {message}");
            await Clients.Caller.SendAsync("ReceiveWebpageRequest", message);
        }


    }
}

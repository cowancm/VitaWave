using Microsoft.AspNetCore.SignalR;
using VitaWave.Common;

namespace VitaWave.WebAPI.Hubs
{
    public class ModuleHub : Hub
    {
        public ModuleHub()
        {

        }

        public async Task ModuleData(EventPacket dataPacket)
        {
            Console.WriteLine("yes!");
        }
    }
}
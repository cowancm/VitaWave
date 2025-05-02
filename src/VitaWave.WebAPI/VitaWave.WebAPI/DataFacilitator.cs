using Microsoft.AspNetCore.SignalR;
using VitaWave.WebAPI.Hubs;

namespace VitaWave.WebAPI
{
    public class DataRouter
    {
        private readonly IHubContext<WebHub> _webHub;

        public DataRouter(IHubContext<WebHub> webHub)
        {
            _webHub = webHub;
        }

        public Task SendDataAsync(object data)
        {
            return _webHub.Clients.All.SendAsync("ReceiveData", data);
        }
    }
}

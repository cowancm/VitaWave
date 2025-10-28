using Microsoft.AspNetCore.SignalR;
using VitaWave.Common;

namespace VitaWave.WebAPI.Hubs
{
    public class ChartHub : Hub
    {

    }

    public static class ChartHubSends
    {
        public static async Task BroadcastUnfilteredPoints(this IHubContext<ChartHub> hub, IEnumerable<PersonPoint> points)
        {
            await hub.Clients.All.SendAsync("OnUnfilteredPoints", points);
        }

        public static async Task BroadcastFilteredPoints(this IHubContext<ChartHub> hub, IEnumerable<PersonPoint> points)
        {
            await hub.Clients.All.SendAsync("OnFilteredPoints", points);
        }

        public static async Task SendOutPlaybackPoints(this IHubContext<ChartHub> hub, PlaybackFile file)
        {
            await hub.Clients.All.SendAsync("OnPlaybackPoints", file);
        }
    }
}

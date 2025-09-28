using VitaWave.Common;

namespace VitaWave.WebAPI.Notifications
{
    public class Message
    {
        public ResultEvent Event { get; set; } = new();

        // for now, just hold an event, more maybe added later
    }
}

// VitaWave.Common/DevCommand.cs
namespace VitaWave.Common
{
    public class DevCommand
    {
        public string Type { get; set; } = "";
        public string Payload { get; set; } = "";
    }

    public static class Commands
    {
        const string SEND_PLAYBACK = "SEND_PLAYBACK"; // payload is the file path
    }
}

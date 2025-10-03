using Serilog;
using System.Data;
using VitaWave.Common;
using VitaWave.Common.TLVs;

namespace VitaWave.Data
{
    public static class SaveDataHelper
    {
        private static readonly string dumpFolderPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "vitawave",
            "datadump"
        );

        static SaveDataHelper()
        {
            if (!Directory.Exists(dumpFolderPath))
            {
                Directory.CreateDirectory(dumpFolderPath);
            }
        }

        public static void Save(List<EventPacket> packets)
        {
            var timenow = DateTime.Now.ToString("yyyyMMddTHHmmss");
            var pathWithFileName = Path.Combine(dumpFolderPath, timenow + ".json");
            var contents = System.Text.Json.JsonSerializer.Serialize(packets);
            File.WriteAllText(pathWithFileName, contents);
            LogTime(packets);
        }

        public static void LogTime(List<EventPacket> packets)
        {
            long time = 0;
            foreach(var packet in packets)
            {
                time += packet.TimeSinceLastMs;
            }
            Log.Information(time.ToString());
        }
    }
}

using Serilog;
using System.Data;
using VitaWave.Common;
using VitaWave.Common.TLVs;

namespace VitaWave.Data
{
    public static class SaveDataHelper
    {

        static List<EventPacket> Packets = new List<EventPacket>();
        const int FRAMES_PER_FILE = 100; // 100 * 55ms = 5.5s
        static object obj = new();
        

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

        public static void Save(EventPacket packet)
        {
            lock(obj)
            {
                Packets.Add(packet);
                if (Packets.Count == FRAMES_PER_FILE)
                {
                    var timenow = DateTime.Now.ToString("yyyyMMddTHHmmss");
                    var contents = System.Text.Json.JsonSerializer.Serialize(Packets);
                    Packets.Clear();
                    var pathWithFileName = Path.Combine(dumpFolderPath, timenow + ".json");
                    File.WriteAllText(pathWithFileName, contents);
                }
            }
            
        }

    }
}

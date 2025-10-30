using Microsoft.AspNetCore.SignalR;
using Serilog;
using VitaWave.Common;
using VitaWave.WebAPI.Hubs;

namespace VitaWave.WebAPI.Playback
{
    public static class PlaybackHelper
    {
        private static readonly string _folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "vitawave"
        );

        private const string FOLDER_NAME = "playback";
        private static readonly string _filePath = Path.Combine(_folder, FOLDER_NAME);

        static PlaybackHelper()
        {
            if (!Directory.Exists(_filePath))
            {
                Directory.CreateDirectory(_filePath);
            }
        }

        public static List<string> GetPlayBackFilesNames()
        {
            return Directory.GetFiles(_filePath).ToList();
        }
        
        public static PlaybackFile? LoadPlaybackFile(string fileName)
        {
            try
            {
                var filePath = Path.Combine(_filePath, fileName);

                if (!File.Exists(filePath))
                {
                    return null;
                }
                string json = File.ReadAllText(filePath);
                var events = System.Text.Json.JsonSerializer.Deserialize<List<EventPacket>>(json);
                var playbackFile = events!.ToPlaybackFile(fileName);

                return playbackFile;
            }
            catch (Exception)
            {
                return null;
            }
        }


        private static object obj = new();
        private static List<EventPacket> events = new();
        private static int numFramesToRecord = 0;
        private static string fileName = "";
        private static bool isRecording = false;
        public static event EventHandler<string>? OnPlaybackFileComplete;
        public static IHubContext<DevHub>? hubContext;

        public static void StartRecording(string fileNam, int framesToRecord)
        {
            if (isRecording)
                return;

            lock (obj)
            {
                events.Clear();
                fileName = fileNam + ".json";
                numFramesToRecord = framesToRecord;
                isRecording = true;
            }
        }

        public static void AddEvent(EventPacket pkt)
        {
            if (!isRecording)
                return;

            lock (obj)
            {
                events.Add(pkt);

                if(events.Count >= numFramesToRecord)
                {
                    var contents = System.Text.Json.JsonSerializer.Serialize(events);
                    var pathWithFileName = Path.Combine(_filePath, fileName);
                    File.WriteAllText(pathWithFileName, contents);
                    events.Clear();
                    isRecording = false;
                }
            }
        }

        private static PlaybackFile ToPlaybackFile(this List<EventPacket> pkts, string fileName)
        {
            var frames = new List<PlaybackFrame>();

            foreach (var pkt in pkts)
            {
                frames.Add(pkt.ToPlaybackFrame());
            }

            var playbackFile = new PlaybackFile
            {
                fileName = fileName,
                frames = frames,
            };
            return playbackFile;
        }

        private static PlaybackFrame ToPlaybackFrame(this EventPacket pkt)
        {
            var frame = new PlaybackFrame
            {
                points = pkt.Points,
            };

            return frame;
        }
    }
}

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
                var playbackFile = System.Text.Json.JsonSerializer.Deserialize<PlaybackFile>(json);
                return playbackFile;
            }
            catch (Exception)
            {
                return null;
            }
        }


        private static object obj = new();
        private static List<PlaybackFrame> playbackFrames = new();
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
                playbackFrames.Clear();
                fileName = fileNam + ".json";
                numFramesToRecord = framesToRecord;
                isRecording = true;
            }
        }

        public static async void AddEvent(EventPacket pkt)
        {
            if (!isRecording)
                return;

            lock (obj)
            {
                var playbackFrame = pkt.ToPlaybackFrame();
                playbackFrames.Add(playbackFrame);

                if(playbackFrames.Count >= numFramesToRecord)
                {
                    var file = new PlaybackFile
                    {
                        frames = playbackFrames,
                        fileName = fileName
                    };
                    var contents = System.Text.Json.JsonSerializer.Serialize(file);
                    var pathWithFileName = Path.Combine(_filePath, fileName);
                    File.WriteAllText(pathWithFileName, contents);
                    playbackFrames.Clear();
                    isRecording = false;
                    hubContext?.Clients.All.SendAsync("PlaybackFileComplete", GetPlayBackFilesNames());
                }
            }
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

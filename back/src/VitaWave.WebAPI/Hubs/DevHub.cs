using Microsoft.AspNetCore.SignalR;
using Serilog;
using VitaWave.Common;
using VitaWave.Data;
using VitaWave.WebAPI.Playback;

namespace VitaWave.WebAPI.Hubs
{
    public class DevHub : Hub
    {
        public readonly DataFacilitator _dataFacilitator;
        public readonly IHubContext<ModuleHub> _moduleHub;

        public DevHub(DataFacilitator dataFacilitator, IHubContext<ModuleHub> moduleHub)
        {
            _dataFacilitator = dataFacilitator;
            _moduleHub = moduleHub;
        }

        public async Task Record(int numFrames, string fileName)
        {
            PlaybackHelper.StartRecording(fileName, numFrames);
        }

        public async Task RequestFiles()
        {
            Log.Information("[DevHub] Requesting playback files...");

            var files = PlaybackHelper.GetPlayBackFilesNames();
            
            if (files.Count == 0)
            {
                Log.Debug("[DevHub] No playback files found.");
            }
            else
            {
                await Clients.All.SendAsync("ReceivePlaybackFileNames", files);
                Log.Debug($"[DevHub] Found {files.Count} playback files.");
            }
        }

        public async Task SendPlaybackFile(string fileName)
        {
            var file = PlaybackHelper.LoadPlaybackFile(fileName);
            if (file != null)
            {
                await Clients.All.SendAsync("ReceivePlaybackFile", file);
                Log.Debug($"[DevHub] Sent playback file: {fileName}");
            }
            else
            {
                Log.Warning($"[DevHub] Failed to load playback file: {fileName}");
            }
        }
    }
}
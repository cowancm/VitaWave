using Serilog;
using System.Collections.Concurrent;

namespace VitaWave.WebAPI.Hubs
{
    public static class ModuleHubState
    {
        public static ConcurrentDictionary<string, string> clientID_moduleID;

        static ModuleHubState() 
        {
            clientID_moduleID = new ConcurrentDictionary<string, string>();
        }

        public static void Add(string clientID, string moduleID)
        {
            clientID_moduleID.TryAdd(clientID, moduleID);

            Log.Debug($"Added client ID {clientID} with moduleID {moduleID}");
        }

        public static string? Remove(string clientID)
        {
            if (clientID_moduleID.TryRemove(clientID, out var moduleID))
            {
                Log.Debug($"Removed client ID {clientID} with moduleID {moduleID}");
                return moduleID;
            }

            return null;
        }
    }
}

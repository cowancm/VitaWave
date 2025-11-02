using Serilog;
using System.Collections.Concurrent;
using VitaWave.Common;

namespace VitaWave.Data
{
    public class DataFacilitator
    {
        public event EventHandler<ResultEvent>? EventRaise;
        private ConcurrentDictionary<string, ModuleTargetTracker> moduleFacilitators = new();

        public void Add(EventPacket dataPacket)
        {
            var moduleId = dataPacket.ModuleID;
            if (!moduleFacilitators.ContainsKey(moduleId))
            {
                var moduleFacilitator = new ModuleTargetTracker(EventRaise);
                moduleFacilitators[moduleId] = moduleFacilitator;
                Log.Information($"Created DataFacilitator for module: {moduleId}");
            }
            moduleFacilitators[moduleId].Add(dataPacket);
        }

        public void Clear(string moduleKey)
        {
            try
            {
                if (moduleFacilitators.TryRemove(moduleKey, out var moduleDataFacilitator))
                {
                    Log.Information($"Cleared DataFacilitator for module: {moduleKey}");
                }
            }
            catch (Exception ex) { }
        }
    }
}

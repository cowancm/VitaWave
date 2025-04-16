using Common.PreProcessed.TLVs;
using Common.Visualizer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Interfaces
{
    public interface ITargetUpdateService
    {
        event EventHandler<List<Target>>? TargetsUpdated;
        void UpdateTargets(List<VisualizerTarget> newTargets);
    }
}

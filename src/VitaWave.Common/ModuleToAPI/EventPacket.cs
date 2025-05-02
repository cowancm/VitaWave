using VitaWave.Common.ModuleToAPI.TLVs;

namespace VitaWave.Common.ModuleToAPI
{
    public record EventPacket
    {
        public List<Point> Points;
        public List<Target> Targets;
        public List<TargetHeight> TargetHeights;

        public EventPacket(List<Point> points, List<Target> targets, List<TargetHeight> heights) 
        {
            Points = points;
            Targets = targets;
            TargetHeights = heights;
        }
    }
}

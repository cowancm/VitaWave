using VitaWave.Common.ModuleToAPI.TLVs;

namespace VitaWave.Common.ModuleToAPI
{
    public record EventPacket
    {
        public List<ParsedPoint> Points { get; set; } = new();
        public List<Target> Targets { get; set; } = new();
        public List<TargetHeight> TargetHeights { get; set; } = new();
        public bool Presence { get; set; } = false;

        public EventPacket(List<ParsedPoint> points, List<Target> targets, List<TargetHeight> heights, bool presence) 
        {
            Points = points;
            Targets = targets;
            TargetHeights = heights;
            Presence = presence;
        }

        public EventPacket() { }

    }
}

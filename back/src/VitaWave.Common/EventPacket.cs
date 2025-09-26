using VitaWave.Common.TLVs;

namespace VitaWave.Common
{
    public record EventPacket
    {
        public List<ParsedPoint> Points { get; set; } = new();
        public List<Target> Targets { get; set; } = new();
        public List<TargetHeight> TargetHeights { get; set; } = new();
        public bool Presence { get; set; } = false;
        public string ModuleID = "";
        public 

        public EventPacket(List<ParsedPoint> points, List<Target> targets, List<TargetHeight> heights, bool presence, string moduleID = "") 
        {
            Points = points;
            Targets = targets;
            TargetHeights = heights;
            Presence = presence;
            ModuleID = moduleID;
        }

        public EventPacket() { }

    }
}

using VitaWave.Common.TLVs;

namespace VitaWave.Common
{
    public record EventPacket
    {
        public List<PointCloudPoint> Points { get; set; } = new();
        public List<Target> Targets { get; set; } = new();
        public bool Presence { get; set; } = false;
        public string ModuleID { get; set; } = "";
        public long TimeSinceLastMs { get; set; } = 0;

        public EventPacket(List<PointCloudPoint> points, List<Target> targets, List<TargetHeight> heights, bool presence, long deltaTime, string moduleID = "")
        {
            Points = points;
            Targets = targets;
            Presence = presence;
            TimeSinceLastMs = deltaTime;
            ModuleID = moduleID;
        }

        public EventPacket() { }

    }
}

using VitaWave.ModuleControl.Parsing.TLVs;
using VitaWave.Common.ModuleToAPI.TLVs;

namespace VitaWave.ModuleControl.Parsing
{
    public record ParsingEvent
    {
        public FrameHeader? FrameHeader { get; set; }
        public List<ParsedPoint>? Points { get; set; }
        public List<Target>? Targets { get; set; }
        public List<TargetHeight>? Heights { get; set; }
        public List<uint>? TargetIndices { get; set; }
        public bool PresenceIndication { get; set; } = false;
        public DateTime? CreationTime { get; set; }
    }
}

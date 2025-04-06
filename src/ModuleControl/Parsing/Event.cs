﻿using ModuleControl.Parsing.TLVs;

namespace ModuleControl.Parsing
{
    public record Event
    {
        public FrameHeader? FrameHeader { get; set; }
        public List<Point>? Points { get; set; }
        public List<Target>? Targets { get; set; }
        public List<TargetHeight>? Heights { get; set; }
        public List<uint>? TargetIndices { get; set; }
        public bool? PresenceIndication { get; set; }
        public DateTime? CreationTime { get; set; }
    }
}

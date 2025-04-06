using System.Runtime.InteropServices;

namespace ModuleControl.Parsing.TLVs
{
    public record FrameHeader
    {
        public const int EXPECTED_SIZE = 32;

        public uint Version           { get; init; }    //size = 4
        public uint TotalPacketLength { get; init; }    //size = 4
        public uint Platform          { get; init; }    //size = 4
        public uint FrameNumber       { get; init; }    //size = 4
        public uint Time              { get; init; }    //size = 4
        public uint NumDetectedObj    { get; init; }    //size = 4
        public uint NumTLVs           { get; init; }    //size = 4
        public uint SubframeNumber    { get; init; }    //size = 4
    }
}

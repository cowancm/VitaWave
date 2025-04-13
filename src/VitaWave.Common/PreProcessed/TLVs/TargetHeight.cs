namespace Common.PreProcessed.TLVs
{
    public record TargetHeight
    {
        public uint TargetID { get; init; }
        public float MaxZ { get; init; }
        public float MinZ { get; init; }
    }
}
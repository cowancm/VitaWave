namespace VitaWave.Common.TLVs
{
    public record TargetHeight
    {
        public uint TargetID { get; set; }
        public float MaxZ { get; set; }
        public float MinZ { get; set; }
    }
}
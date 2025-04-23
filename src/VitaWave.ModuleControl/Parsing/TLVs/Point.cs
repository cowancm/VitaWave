namespace VitaWave.ModuleControl.Parsing.TLVs
{
    public record Point
    {
        public double X { get; init; }
        public double Y { get; init; }
        public double Z { get; init; }
        public double Doppler { get; init; }
        public double Snr { get; init; }
        public uint TID { get; set; }
    }
}

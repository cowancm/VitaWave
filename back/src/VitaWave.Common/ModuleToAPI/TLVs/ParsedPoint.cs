using VitaWave.Common.APIToWebserver;

namespace VitaWave.Common.ModuleToAPI.TLVs
{
    public record ParsedPoint
    {
        public required double X { get; init; }
        public required double Y { get; init; }
        public double Z { get; init; }
        public uint TID { get; set; }
        public double Doppler { get; init; }
        public double SNR { get; init; }
    }
}

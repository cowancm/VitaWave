using VitaWave.Common.APIToWebserver;

namespace VitaWave.Common.ModuleToAPI.TLVs
{
    public record ParsedPoint : PersonPoint
    {
        public double Z { get; init; }
        public double Doppler { get; init; }
        public double SNR { get; init; }
    }
}

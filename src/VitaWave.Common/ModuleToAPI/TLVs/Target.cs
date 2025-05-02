using VitaWave.Common.APIToWebserver;

namespace VitaWave.Common.ModuleToAPI.TLVs
{
    public record Target : PersonPoint
    {
        public float Z { get; init; }
        public float VelX { get; init; }
        public float VelY { get; init; }
        public float VelZ { get; init; }
        public float AccX { get; init; }
        public float AccY { get; init; }
        public float AccZ { get; init; }
        public List<float>? Ec { get; init; } // Error covariance matrix
        public float G { get; init; }
        public float ConfidenceLevel { get; init; }
    }
}
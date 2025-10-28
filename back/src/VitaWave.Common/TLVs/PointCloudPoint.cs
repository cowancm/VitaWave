using System.Text.Json.Serialization;

namespace VitaWave.Common.TLVs
{
    public record PointCloudPoint
    {
        [JsonPropertyName("x")]
        public required double X { get; init; }

        [JsonPropertyName("y")]
        public required double Y { get; init; }

        [JsonPropertyName("z")]
        public double Z { get; init; }

        [JsonPropertyName("tid")]
        public uint TID { get; set; }

        [JsonPropertyName("doppler")]
        public double Doppler { get; init; }

        [JsonPropertyName("snr")]
        public double SNR { get; init; }
    }
}

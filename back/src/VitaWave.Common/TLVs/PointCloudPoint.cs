using System.Drawing;
using System.Text.Json.Serialization;

namespace VitaWave.Common.TLVs
{
    public record PointCloudPoint : GenericPoint
    {
        [JsonPropertyName("tid")]
        public uint TID { get; set; }

        [JsonPropertyName("doppler")]
        public double Doppler { get; init; }

        [JsonPropertyName("snr")]
        public double SNR { get; init; }

        [JsonIgnore]
        public int ClusterId { get; set; } = -1;

        [JsonIgnore]
        public bool Visited { get; set; } = false;
    }
}

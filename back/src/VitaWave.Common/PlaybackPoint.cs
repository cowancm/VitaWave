using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using VitaWave.Common.TLVs;

namespace VitaWave.Common
{

    public class PlaybackFile
    {
        [JsonPropertyName("frames")]
        public List<PlaybackFrame> frames { get; set; } = new();

        [JsonPropertyName("fileName")]
        public string fileName { get; set; } = "";
    }

    public class PlaybackFrame
    {
        [JsonPropertyName("points")]
        public List<PointCloudPoint> points { get; set; } = new();

    }

    // From other file.
    //public record PointCloudPoint
    //{
    //    [JsonPropertyName("x")]
    //    public required double X { get; init; }

    //    [JsonPropertyName("y")]
    //    public required double Y { get; init; }

    //    [JsonPropertyName("z")]
    //    public double Z { get; init; }

    //    [JsonPropertyName("tid")]
    //    public uint TID { get; set; }

    //    [JsonPropertyName("doppler")]
    //    public double Doppler { get; init; }

    //    [JsonPropertyName("snr")]
    //    public double SNR { get; init; }
    //}
}

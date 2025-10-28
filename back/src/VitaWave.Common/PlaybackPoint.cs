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

        [JsonPropertyName("timeOffset")]
        public float timeOffset { get; set; } //how long from the first frame in the set (the first frame should be 0!) in seconds!
    }
}

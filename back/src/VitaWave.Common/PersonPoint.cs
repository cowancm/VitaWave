using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Timers;

namespace VitaWave.Common
{
    public record PersonPoint
    {
        public const string UNKNOWN_STATUS = "Unknown";
        public const uint UNKNOWN_TID = 255;

        [JsonPropertyName("x")]
        public float X { get; set; }

        [JsonPropertyName("y")]
        public float Y { get; set; }

        [JsonPropertyName("tid")]
        public uint TID { get; set; } = UNKNOWN_TID; // Unknown

        [JsonPropertyName("status")]
        public string Status { get; set; } = UNKNOWN_STATUS;
    }
}

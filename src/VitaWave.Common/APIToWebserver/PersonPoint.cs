using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace VitaWave.Common.APIToWebserver
{
    public record PersonPoint
    {
        public const string UNKNOWN_STATUS = "Unknown";
        public const uint UNKNOWN_TID = 255;

        public required float X { get; set; }
        public required float Y { get; set; }
        public uint TID { get; set; } = UNKNOWN_TID; // Unknown
        public string Status { get; set; } = UNKNOWN_STATUS;
    }
}

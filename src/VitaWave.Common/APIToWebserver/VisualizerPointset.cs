using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace VitaWave.Common.APIToWebserver
{
    public class PersonPoint
    {
        public const string UNKNOWN_STATUS = "Unknown";

        public required double X { get; set; }
        public required double Y { get; set; }
        public required uint TID { get; set; }
        public string Status { get; set; } = UNKNOWN_STATUS;
    }
}

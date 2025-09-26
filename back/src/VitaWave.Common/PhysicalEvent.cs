using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VitaWave.Common
{
    public class PhysicalEvent
    {
        int Severity = 1;
        public string ModuleID = "";
        string Event = "";

        public static PhysicalEvent Standing => new PhysicalEvent()
        {
            Severity = 1,
            Event = "STANDING"
        };

        public static PhysicalEvent Sitting => new PhysicalEvent()
        {
            Severity = 1,
            Event = "Sitting"
        };

        public static PhysicalEvent Active => new PhysicalEvent()
        {
            Severity = 1,
            Event = "Active"
        };

        public static PhysicalEvent Fall => new PhysicalEvent()
        {
            Severity = 5,
            Event = "Fall"
        };


        // no detection in a long time
        public static PhysicalEvent Dormant => new PhysicalEvent()
        {
            Severity = 2,
            Event = "Dormant"
        };
    }
}

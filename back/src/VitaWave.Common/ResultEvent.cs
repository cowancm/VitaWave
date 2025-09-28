using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VitaWave.Common
{
    public class ResultEvent
    {
        public int Severity = 1;
        public string Event = "";
        public string ModuleID = "";
        public DateTime DateTime = DateTime.Now;

        public string DateTimeString => DateTime.ToString(); 

        public static ResultEvent Standing => new ResultEvent()
        {
            Severity = 1,
            Event = "Standing"
        };

        public static ResultEvent Sitting => new ResultEvent()
        {
            Severity = 1,
            Event = "Sitting"
        };

        public static ResultEvent Active => new ResultEvent()
        {
            Severity = 1,
            Event = "Active"
        };

        public static ResultEvent Fall => new ResultEvent()
        {
            Severity = 5,
            Event = "Fall"
        };


        // no detection in a long time
        public static ResultEvent Dormant => new ResultEvent()
        {
            Severity = 2,
            Event = "Dormant"
        };

        // Person is in the same place for a long time
        public static ResultEvent InPlace => new ResultEvent()
        {
            Severity = 5,
            Event = "In Place"
        };
    }
}

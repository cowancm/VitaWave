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
        public int TID = 255;
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

        public static ResultEvent Laying => new ResultEvent()
        {
            Severity = 1,
            Event = "Laying"
        };

        public static ResultEvent Unknown => new ResultEvent()
        {
            Severity = 1,
            Event = "Unknown"
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
    }
}

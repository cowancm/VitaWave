using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using VitaWave.Common.TLVs;

namespace VitaWave.Common
{
    public class ResultEvent
    {
        public string ModuleID { get; set; } = "";
        public required PersonPoint Target { get; set; }
        public ResultID ResultId { get; set; } = ResultID.Unknown;

        [JsonIgnore]
        public string TID => Target?.TID.ToString() ?? "Unknown";

        public ResultType GetTypeResult(ResultID enumId)
        {
            var id = (int)enumId;
            if (id >= 30)
                return ResultType.Fall;
            else if (id >= 20)
                return ResultType.Meta;
            else if (id >= 10)
                return ResultType.Dynamic;
            else if (id >= 1)
                return ResultType.Static;
            else
                return ResultType.Unknown;
        }
    }

    public enum ResultType
    {
        Unknown,
        Static,
        Dynamic,
        Meta,
        Fall
    }

    public enum ResultID
    {
        Unknown = 0,
        Standing = 1,
        Sitting = 2,
        Laying = 3,
        Active = 10,
        NonDetection10Hr = 20, // No detection for an extended period of time
        Inactive2Hr = 21, // Detected, but position more or less same for an extended period of time
        Fall = 30,
    }
}

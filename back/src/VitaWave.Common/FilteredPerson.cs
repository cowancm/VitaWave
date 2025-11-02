using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VitaWave.Common.TLVs;

namespace VitaWave.Common
{
    public class FilteredPerson
    {
        public Target Target { get; set; } = new();
        public long TimeSinceLastMs { get; set; } = 0;
    }
}

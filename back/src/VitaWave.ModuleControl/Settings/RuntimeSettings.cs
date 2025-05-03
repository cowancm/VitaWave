using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VitaWave.ModuleControl.Settings
{
    public class RuntimeSettings
    {
        public required string DataPortName { get; set; }
        public required string CliPortName { get; set; }
        public required int DataBaud { get; set; }
        public required int CliBaud { get; set; }

    }
}

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
        public required int DataBaudRate { get; set; }
        public required int CliBaudRate { get; set; }

        public static RuntimeSettings Default 
        { 
            get 
            {
                return new RuntimeSettings()
                {
                    DataPortName = "COM9",
                    CliPortName = "COM8",
                    DataBaudRate = 921600,
                    CliBaudRate = 115200
                };
            } 
        }

    }
}

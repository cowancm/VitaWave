using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace VitaWave.ModuleControl.Settings
{
    public class Config
    {
        public required string DataPortName { get; set; }
        public required string CliPortName { get; set; }
        public required int DataBaud { get; set; }
        public required int CliBaud { get; set; }
        public required string Identifier { get; set; }
        public required string API_Server_IP { get; set; }
        public required int Port { get; set; }

        public static Config Default 
        { 
            get {
                return new Config()
                {
                    DataPortName = string.Empty,
                    CliPortName = string.Empty,
                    DataBaud = 921600,
                    CliBaud = 115200,
                    Identifier = Guid.NewGuid().ToString("N").Substring(0, 10),
                    API_Server_IP = "192.168.10.1",
                    Port = 5000
                };
            } 
        }
    }
}

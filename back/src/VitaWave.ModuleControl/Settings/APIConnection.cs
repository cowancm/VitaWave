using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace VitaWave.ModuleControl.Settings
{
    public class APIConnection
    {
        public required string API_Url { get; set; }

        public static APIConnection Default
        {
            get
            {
                return new APIConnection()
                {
                    API_Url = "http://localhost:5278/module"
                };
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ModuleControl.Parsing.TLVs
{ 
    public record Point
    {   
        public double X { get; init; }
        public double Y { get; init; }
        public double Z { get; init; }
        public double Doppler { get; init; }
        public double Snr { get; init; }
    }
}

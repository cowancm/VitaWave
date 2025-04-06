using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModuleControl.Parsing.TLVs
{
    public static class TLV_Constants
    {
        public enum TLV_TYPE
        {
            POINT_CLOUD = 1020,
            TARGET_LIST = 1010,
            TARGET_INDEX = 1011,
            TARGET_HEIGHT = 1012,
            PRESENCE_INDICATION = 1021
        }

        public static readonly uint[] DEFINED_TLVS = { 1020, 1010, 1011, 1012, 1021 };
    }
}

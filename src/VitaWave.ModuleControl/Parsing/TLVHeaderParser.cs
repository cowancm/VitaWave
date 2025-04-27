using System.Runtime.InteropServices;
using static VitaWave.ModuleControl.Parsing.TLVs.TLV_Constants;

namespace VitaWave.ModuleControl.Parsing
{
    public static class TLVHeaderParser
    {
        public const int HEADER_LENGTH = 8;
        public static (TLV_TYPE, int) GetHeaderTypeSize(Span<byte> data)
        {
            var type = (TLV_TYPE)MemoryMarshal.Read<uint>(data.Slice(0, 4));
            var tlv_length = MemoryMarshal.Read<int>(data.Slice(4, 4));

            return (type, tlv_length);
        }
    }
}

namespace VitaWave.ModuleControl.Parsing.TLVs
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

        public static readonly int[] MAGIC_WORD = { 2, 1, 4, 3, 6, 5, 8, 7 };

        public const int FULL_FRAME_HEADER_SIZE = 40;
    }
}

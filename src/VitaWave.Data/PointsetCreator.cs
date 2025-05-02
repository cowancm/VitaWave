using VitaWave.Common.APIToWebserver;
using VitaWave.Common.ModuleToAPI;
using VitaWave.Common.ModuleToAPI.TLVs;

namespace VitaWave.Data
{
    public static class PointsetCreator
    {
        public static List<PersonPoint> ToPersonPointSet(this EventPacket eventPacket)
        {
            var points = new List<PersonPoint>();
            points.AddRange(eventPacket.Points);
            return points;
        }

        public static List<PersonPoint> ToPersonPointSet(this List<ParsedPoint> parsedPoints)
        {
            var points = new List<PersonPoint>();
            points.AddRange(parsedPoints);
            return points;
        }

        //TODO: polymorph ToPersonPointSet() to extent to other event types
    }
}

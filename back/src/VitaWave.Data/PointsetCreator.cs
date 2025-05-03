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
            points.AddRange(eventPacket.Targets);
            return points;
        }

        public static List<PersonPoint> ToPersonPointSet(this List<Target> targets)
        {
            var points = new List<PersonPoint>();
            points.AddRange(targets);
            return points;
        }

        //TODO: polymorph ToPersonPointSet() to extent to other event types
    }
}

using VitaWave.Common.APIToWebserver;
using VitaWave.Common.ModuleToAPI;

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

        //TODO: polymorph ToPersonPointSet() to extent to other event types
    }
}

using VitaWave.Common;
using VitaWave.Common.TLVs;

namespace VitaWave.Data
{
    public static class PointsetCreator
    {
        public static List<PersonPoint> ToPersonPointSet(this EventPacket eventPacket)
        {
            var points = new List<PersonPoint>();
            
            for (int i = 0; i < eventPacket.Targets.Count; i++)
            {
                var target = (PersonPoint)eventPacket.Targets[i];
                target.Status = $"Acceleration X: {eventPacket.Targets[i].AccX}m/s^2";
                points.Add(target);
            }

            points.AddRange(eventPacket.Targets);
            return points;
        }

        public static List<PersonPoint> ToPersonPointSet(this List<Target> targets)
        {
            var points = new List<PersonPoint>();
            points.AddRange(targets);
            return points;
        }
    }
}

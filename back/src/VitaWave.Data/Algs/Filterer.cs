using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VitaWave.Common;

namespace VitaWave.Data.Algs
{
    public static class Filterer
    {
        public static List<FilteredPerson> Filter(this EventPacket e)
        {
            // For now, all this does is grab all the info out of the packet for each TID :)
            // TODO: Implement fancy stuff

            return DoBadWay(e);
        }

        private static List<FilteredPerson> DoBadWay(EventPacket e)
        {
            var list = new List<FilteredPerson>(e.Targets.Count);

            try
            {
                for (int i = 0; i < e.Targets.Count; i++)
                {
                    if (e.Targets[i].TID >= 253) // Unknown TID according to TI documentation
                        continue;

                    list[i] = new FilteredPerson();
                    list[i].TID = e.Targets[i].TID.ToString();
                    list[i].TargetHeight = e.TargetHeights[i];
                    list[i].Points = e.Points.Where(p => p.TID == e.Targets[i].TID).ToList();
                    list[i].TimeSinceLastMs = e.TimeSinceLastMs;
                }
            }
            catch (Exception ex)
            {
                Log.Fatal("Exception in Filterer: " + ex.Message);
            }

            return list;
        }
    }
}

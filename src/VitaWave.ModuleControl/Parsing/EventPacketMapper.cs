using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VitaWave.Common.ModuleToAPI;
using VitaWave.Common.ModuleToAPI.TLVs;

namespace VitaWave.ModuleControl.Parsing
{
    internal static class EventPacketMapper
    {

        public static EventPacket Map(ParsingEvent pEvent)
        {
            return new EventPacket(pEvent.Points ?? new List<Point>(), pEvent.Targets ?? new List<Target>(), pEvent.Heights ?? new List<TargetHeight>());
        }

    }
}

using Serilog;
using System.Collections.Concurrent;
using VitaWave.Common;

namespace VitaWave.Data
{
    public class DataFacilitator
    {
        public event EventHandler<ResultEvent>? EventRaise;
        public void Add(ResultEvent dataPacket)
        {
            Task.Run(() => EventRaise?.Invoke(this, dataPacket));
        }
    }
}

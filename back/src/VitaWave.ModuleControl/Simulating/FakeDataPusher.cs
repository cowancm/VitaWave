using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VitaWave.Common;
using VitaWave.ModuleControl.Interfaces;

namespace VitaWave.ModuleControl.Simulating
{
    public class FakeDataPusher
    {
        private readonly ISignalRClient _client;
        public FakeDataPusher(ISignalRClient client)
        {
            _client = client;
        }

        public async Task PushData(EventPacket? packet = null)
        {
            if (packet == null)
                packet = FakeData.CreateSingle();

            await _client.SendDataAsync(packet);
        }
    }
}

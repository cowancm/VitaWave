using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VitaWave.Common.ModuleToAPI;
using VitaWave.Common.ModuleToAPI.TLVs;
using VitaWave.ModuleControl.Interfaces;

namespace VitaWave.ModuleControl.Simulating
{
    public class FakeDataPusher
    {

        private readonly ISignalRClient _client;
        Random _rnd = new Random();

        public FakeDataPusher(ISignalRClient client)
        {
            _client = client;
        }

        public async Task PushData()
        {
            await _client.SendDataAsync(CreateFakeEventPacket());
        }

        private EventPacket CreateFakeEventPacket()
        {
            return new EventPacket()
            {
                Points = new(),
                Presence = true,
                TargetHeights = new(),
                Targets = new()
                {
                    new Target()
                    {
                        X = (float) _rnd.NextDouble() * -5,
                        Y = (float) _rnd.NextDouble() * 10,
                        TID = (uint) _rnd.Next(0,254)
                    },
                    new Target()
                    {
                        X = (float) _rnd.NextDouble() * 5,
                        Y = (float) _rnd.NextDouble() * 10,
                        TID = (uint) _rnd.Next(0,254)
                    },
                    new Target()
                    {
                        X = (float) _rnd.NextDouble() * -5,
                        Y = (float) _rnd.NextDouble() * 10,
                        TID = (uint) _rnd.Next(0,254)
                    },
                    new Target()
                    {
                        X = (float) _rnd.NextDouble() * 5,
                        Y = (float) _rnd.NextDouble() * 10,
                        TID = (uint) _rnd.Next(0,254)
                    }
                }
            };
        }
    }
}

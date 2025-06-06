using VitaWave.Common;
using VitaWave.Common.TLVs;

namespace VitaWave.ModuleControl.Simulating
{
    internal class FakeData
    {
        public static EventPacket CreateSingle()
        {
            var rand = new Random();

            return new EventPacket()
            {
                Points = new(),
                Presence = true,
                TargetHeights = new(),
                Targets = new()
                {
                    new Target()
                    {
                        X = (float) rand.NextDouble() * -5,
                        Y = (float) rand.NextDouble() * 10,
                        TID = (uint) rand.Next(0,254)
                    },
                    new Target()
                    {
                        X = (float) rand.NextDouble() * 5,
                        Y = (float) rand.NextDouble() * 10,
                        TID = (uint) rand.Next(0,254)
                    },
                    new Target()
                    {
                        X = (float) rand.NextDouble() * -5,
                        Y = (float) rand.NextDouble() * 10,
                        TID = (uint) rand.Next(0,254)
                    },
                    new Target()
                    {
                        X = (float) rand.NextDouble() * 5,
                        Y = (float) rand.NextDouble() * 10,
                        TID = (uint) rand.Next(0,254)
                    }
                }
            };
        }

    }
}

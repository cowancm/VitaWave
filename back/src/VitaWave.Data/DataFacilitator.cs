using Serilog;
using System.Collections.Concurrent;
using System.Numerics;
using System.Reflection.Metadata.Ecma335;
using VitaWave.Common;

namespace VitaWave.Data
{
    public class DataFacilitator
    {
        public readonly Dictionary<string, ConcurrentQueue<EventPacket>> _instances = new();
        const int MAX_EVENT_WINDOW = 100; // store the last 100 module events for alg
        private bool dataSave = true;
        public event EventHandler<ResultEvent>? EventRaise;

        public void Add(EventPacket packet)
        {
            try
            {
                var moduleID = packet.ModuleID;
                if (_instances.TryGetValue(moduleID, out var dataQueue))
                {
                    dataQueue.Enqueue(packet);
                    if (dataQueue.Count > MAX_EVENT_WINDOW)
                    {
                        dataQueue.TryDequeue(out var _);
                    }

                    if (dataQueue.Count == MAX_EVENT_WINDOW)
                    {
                        var events = dataQueue.ToList();

                        if (dataSave)
                        {
                            SaveDataHelper.Save(events);
                        }

                        OnNewData(events);
                    }
                }
                else
                {
                    dataQueue = new ConcurrentQueue<EventPacket>();
                    _instances.Add(moduleID, dataQueue);
                    dataQueue.Append(packet);
                }
            }
            catch (Exception ex)
            { }
        }


        private Dictionary<int, Person> trackedPersons = new(); // key = TID
        public void OnNewData(List<Common.EventPacket> frames)
        {
            foreach (var frame in frames)
            {
                double deltaTime = frame.TimeSinceLastMs / 1000.0; // convert to seconds

                if (frame.Targets == null || frame.Targets.Count == 0)
                    continue;

                foreach (var target in frame.Targets)
                {
                    if (!trackedPersons.ContainsKey((int)target.TID))
                        trackedPersons[(int)target.TID] = new Person();

                    var person = trackedPersons[(int)target.TID];
                    Vector3 radarPos = new((float)target.X, (float)target.Y, (float)target.Z);
                    person.UpdatePosition(radarPos, deltaTime);
                    var posture = person.ClassifyPosture();
                    posture.TID = (int)target.TID;
                    Notify(posture);
                }
            }
        }

        public void Notify(ResultEvent e)
        {
            Log.Information("Event: " + e.Event + "   " + "TID: " + e.TID);

            //if (EventRaise?.GetInvocationList() != null)
            //    EventRaise.Invoke(this, e);
        }


        public void Clear(string key)
        {
            try
            {
                _instances.Remove(key);
            } catch (Exception ex) { } 
        }
    }
}

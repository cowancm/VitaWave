using System.Collections.Concurrent;
using VitaWave.Common;

namespace VitaWave.Data
{
    public class DataFacilitator
    {
        public readonly Dictionary<string, ConcurrentQueue<EventPacket>> _instances = new();
        const int MAX_EVENT_WINDOW = 10; // store the last 100 module events for alg

        public event EventHandler<List<EventPacket>> DataReceived = delegate { };

        public DataFacilitator() { }

        public void Add(EventPacket packet)
        {
            try
            {
                var moduleID = packet.ModuleID;
                if (_instances.TryGetValue(moduleID, out var dataList))
                {
                    dataList.Enqueue(packet);
                    if (dataList.Count > MAX_EVENT_WINDOW)
                    {
                        dataList.TryDequeue(out var _);
                    }

                    if (dataList.Count == MAX_EVENT_WINDOW)
                    {
                        var handlers = DataReceived?.GetInvocationList();
                        if (handlers != null)
                        {
                            foreach (EventHandler<List<EventPacket>> handler in handlers)
                            {
                                Task.Run(() => handler(this, dataList.ToList())); //make a copy, sends to dataProcessor
                            }
                        }
                    }
                }
                else
                {
                    dataList = new ConcurrentQueue<EventPacket>();
                    _instances.Add(moduleID, dataList);
                    dataList.Append(packet);
                }
            }
            catch (Exception ex)
            { }
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

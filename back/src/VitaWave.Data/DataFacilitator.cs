using System.Collections.Concurrent;
using System.Reflection.Metadata.Ecma335;
using VitaWave.Common;

namespace VitaWave.Data
{
    public class DataFacilitator
    {
        public readonly Dictionary<string, ConcurrentQueue<EventPacket>> _instances = new();
        public readonly DataProcessor _dataProcessor;
        const int MAX_EVENT_WINDOW = 100; // store the last 100 module events for alg
        private bool dataSave = true;

        public DataFacilitator(DataProcessor dataProcessor)
        {
            _dataProcessor = dataProcessor;
        }

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

                        _dataProcessor.NewData(events); // copy to a list instead of pass by ref
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

        public void Clear(string key)
        {
            try
            {
                _instances.Remove(key);
            } catch (Exception ex) { } 
        }
    }
}

using Serilog;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using VitaWave.ModuleControl.Console;
using VitaWave.ModuleControl.Interfaces;
using VitaWave.ModuleControl.Parsing.TLVs;

namespace VitaWave.ModuleControl.Parsing
{
    public class SerialDataProcessor : ISerialProcessor
    {
        public bool IsRunning => _worker != null;

        private ConcurrentQueue<(byte[] Buffer, FrameHeader Header)> _frameQueue = new ConcurrentQueue<(byte[], FrameHeader)>();
        private Task? _worker = null;
        private SemaphoreSlim _signal = new SemaphoreSlim(0);
        private CancellationTokenSource? _cts = null;

        private readonly IDataAggregator _dataAggregator;

        public SerialDataProcessor(IDataAggregator dataAggregator) 
        {
            _dataAggregator = dataAggregator;
        }

        public void AddToQueue(byte[] buffer, FrameHeader header)
        {
            _frameQueue.Enqueue((buffer, header));
            _signal.Release();
        }

        public void Run(CancellationToken ct)
        {
            if (_worker == null)
            {
                _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                _worker = Task.Run(async () => await WaitForNewFrames(_cts.Token));
            }
        }

        public void Stop()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }

        private async Task WaitForNewFrames(CancellationToken ct)
        {
            try
            {
                Thread.CurrentThread.Priority = ThreadPriority.Highest;

                while (!ct.IsCancellationRequested)
                {
                    while (_frameQueue.TryDequeue(out var newData))
                    {
                        CreateAndNotifyFrame(newData.Buffer, newData.Header);
                    }

                    await _signal.WaitAsync(ct);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in frame processing worker");
            }
            finally
            {
                _worker = null;
            }
        }


        
        Event? _old;
        /// <summary>
        /// Target indices come from frame n+1 for frame n, therefore, we wait for the next frame so we can get this data, and 
        /// apply them to the old object before we ship the old object out. So every frame is sent on the next call of this fn.
        /// If there are failures, we apply null where need be so we don't confuse later data aggregation. Basically, if it's bad
        /// data, we nuke this one and the last
        /// </summary>
        /// <param name="tlvBuffer"></param>
        /// <param name="frameHeader"></param>
        private void CreateAndNotifyFrame(byte[] tlvBuffer, FrameHeader frameHeader)
        {
            try
            {
                var newEvent = FrameParser.CreateEvent(tlvBuffer, frameHeader);
                if (newEvent == null)
                {
                    _old = null;
                    Log.Error("Resultant frame is null");
                    return;
                }
                if (newEvent.TargetIndices != null)
                {
                    if (_old?.Points?.Count != newEvent.TargetIndices.Count)
                    {
                        _old = null;
                        Log.Error("Frame target indices doesn't match expected number of points");
                        return;
                    }
                    for (int i = 0; i < newEvent.TargetIndices.Count; i++)
                    {
                        _old!.Points![i].TID = newEvent.TargetIndices[i];
                    }
                }
                if (_old != null)
                {
                    //BIG TODO: send this to the aggregator

                    //for now for testing, let's send this to the console
                    ConsoleHelpers.PrintTargetIndication(newEvent);
                }
                _old = newEvent;
            }
            catch (Exception e)
            {
                _old = null;
                Log.Error(e, "Bad Frame");
            }
        }

        public void Dispose()
        {
            Stop();
            _signal.Dispose();
        }
    }
}
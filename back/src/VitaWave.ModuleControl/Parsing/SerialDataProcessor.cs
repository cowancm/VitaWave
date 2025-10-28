using Microsoft.AspNetCore.SignalR.Client;
using Serilog;
using System.Diagnostics;
using VitaWave.Common;
using VitaWave.ModuleControl.Console;
using VitaWave.ModuleControl.Interfaces;
using VitaWave.ModuleControl.Parsing.TLVs;
using VitaWave.ModuleControl.Settings;

namespace VitaWave.ModuleControl.Parsing
{
    public class SerialDataProcessor : ISerialProcessor
    {
        public bool IsRunning => _worker != null || _worker != Task.CompletedTask;

        private (byte[] Buffer, FrameHeader Header)[] _frameBuffer = new (byte[], FrameHeader)[100];
        private int _writeIndex = 0;
        private int _readIndex = 0;
        private bool _bufferFull = false;
        private Task? _worker = null;
        private SemaphoreSlim _signal = new SemaphoreSlim(0);
        private CancellationTokenSource? _cts = null;

        private string _moduleID = "";

        private readonly ISignalRClient _client;

        public SerialDataProcessor(ISignalRClient client)
        {
            _client = client;
            _moduleID = SettingsManager.GetConfigSettings().Identifier;
        }

        public void AddToQueue(byte[] buffer, FrameHeader header)
        {
            _frameBuffer[_writeIndex] = (buffer, header);
            _writeIndex = (_writeIndex + 1) % _frameBuffer.Length;

            if (_writeIndex == _readIndex)
            {
                _bufferFull = true;
                _readIndex = (_readIndex + 1) % _frameBuffer.Length;
                Log.Warning("Frame buffer overflow - dropping oldest frame");
            }

            _signal.Release();
        }

        object _lock = new();

        public void Run()
        {
            lock (_lock)
            {
                if (_cts == null)
                {
                    _cts = new CancellationTokenSource();
                }

                if (_worker == null)
                {
                    _worker = Task.Run(async () => await WaitForNewFrames(_cts.Token));
                }
            }
        }

        public void Stop()
        {
            lock (_lock)
            {
                _cts?.Cancel();
                _cts?.Dispose();
                _cts = null;
            }
        }

        private async Task WaitForNewFrames(CancellationToken ct)
        {
            try
            {
                Thread.CurrentThread.Priority = ThreadPriority.Highest;

                while (!ct.IsCancellationRequested)
                {
                    // Process all available frames
                    while (_readIndex != _writeIndex || _bufferFull)
                    {
                        var data = _frameBuffer[_readIndex];
                        _readIndex = (_readIndex + 1) % _frameBuffer.Length;
                        _bufferFull = false;

                        CreateNewSendLast(data.Buffer, data.Header);
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

//        private async void CreateNewSendLast(byte[] tlvBuffer, FrameHeader header)
//        {
//            var e = FrameParser.CreateEvent(tlvBuffer, header);
//            if (e != null) 
//            {

//                if (_client.Status == HubConnectionState.Connected)
//                    _ = _client.SendDataAsync(new EventPacket(e.Points ?? new(),
//                                                              e.Targets ?? new(),
//                                                              e.Heights ?? new(),
//                                                              e.PresenceIndication,
//                                                              _moduleID));
//#if DEBUG
//                ConsoleHelpers.PrintTargetIndication(e);
//#endif
//            }
//        }



        ParsingEvent? _old;
        long _olderMs;  // timestamp of packet before _old
        long _lastMs;   // timestamp of _old
        readonly Stopwatch sw = Stopwatch.StartNew();
        private void CreateNewSendLast(byte[] tlvBuffer, FrameHeader frameHeader)
        {
            try
            {
                var newEvent = FrameParser.CreateEvent(tlvBuffer, frameHeader);
                var nowMs = sw.ElapsedMilliseconds;

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
        #if DEBUG
                    ConsoleHelpers.PrintTargetIndication(newEvent);
        #endif
                    if (_client.Status == HubConnectionState.Connected)
                    {
                        // Delta = time between _old and the one before it
                        var delta = _lastMs - _olderMs;

                        _ = _client.SendDataAsync(new EventPacket(
                            _old.Points ?? new(),
                            _old.Targets ?? new(),
                            _old.Heights ?? new(),
                            _old.PresenceIndication,
                            delta
                        ));
                    }
                }

                // Shift timestamps forward
                _olderMs = _lastMs; 
                _lastMs = nowMs;    
                _old = newEvent;    // prepare for next send
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
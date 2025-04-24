using System.Text;
using System.IO.Ports;
using Serilog;
using VitaWave.ModuleControl.Utils;
using VitaWave.ModuleControl.Parsing.TLVs;
using VitaWave.Common.Interfaces;
using VitaWave.ModuleControl.Interfaces;

namespace VitaWave.ModuleControl.Parsing
{
    public sealed class ModuleIO : IModuleIO
    {
        public ModuleIO(IRuntimeSettingsManager settingsManager)
        {
            _settingsManager = settingsManager;

            var settings = _settingsManager.GetSettings();

            if (settings != null)
            {
                DataPortName = settings.DataPortName;
                CliPortName = settings.CliPortName;
                DataBaud = settings.DataBaudRate;
                CliBaud = settings.CliBaudRate;
            }

            Status = State.AwaitingPortInit;
        }

        public event EventHandler? OnConnectionLost;
        public State Status { get; private set; }
        public string? DataPortName { get; private set; }
        public string? CliPortName { get; private set; }
        public int DataBaud { get; private set; }
        public int CliBaud { get; private set; }

        private readonly IRuntimeSettingsManager _settingsManager;
        private SerialPort? _dataPort;
        private SerialPort? _cliPort;

        public void InitializePorts()
        {
            try
            {
                _dataPort = new SerialPort(DataPortName, DataBaud, Parity.None, 8, StopBits.One)
                {
                    WriteTimeout = 5000,
                    ReadTimeout = 1000,
                    ReadBufferSize = 8192
                };

                _cliPort = new SerialPort(CliPortName, CliBaud, Parity.None, 8, StopBits.One)
                {
                    WriteTimeout = 5000,
                    Encoding = Encoding.UTF8,
                };

                _dataPort.DataReceived += OnDataRecieved;

                _dataPort.Open();
                _cliPort.Open();
            }
            catch (Exception ex)
            {
                Close();
                Log.Error(ex, $"Error Initializing ports." +
                            $"\nPorts: DATA[{DataPortName ?? "NULL"}], CLI[{CliPortName ?? "NULL"}]");
            }
            Status = State.Paused;
        }

        public State Run()
        {
            Status = State.Running;
            return Status;
        }

        public void Pause()
        {
            Status = State.Paused; //This only pasuses sending/processing new frames, serial buffers are still getting read
        }

        public State Close()
        {
            OnConnectionLost?.Invoke(this, EventArgs.Empty);
            Status = State.AwaitingPortInit;
            ClosePorts();
            return Status;
        }

        private void ClosePorts()
        {
            if (_dataPort != null)
            {
                if (_dataPort.IsOpen)
                {
                    try { _dataPort.Close(); } catch { /* ignore errors during close */ }
                }
                _dataPort.Dispose();
                _dataPort = null;
            }

            if (_cliPort != null)
            {
                if (_cliPort.IsOpen)
                {
                    try { _cliPort.Close(); } catch { /* ignore errors during close */ }
                }
                _cliPort.Dispose();
                _cliPort = null;
            }
        }


        Queue<int> _magicWordQueue = new Queue<int>();
        private void OnDataRecieved(object sender, SerialDataReceivedEventArgs e)
        {

            try
            {
                if (_dataPort!.BytesToRead >= 4)
                {
                    int byteValue = _dataPort.ReadByte();
                    _magicWordQueue.Enqueue(byteValue);

                    if (_magicWordQueue.Count > TLV_Constants.MAGIC_WORD.Length)
                    {
                        _magicWordQueue.Dequeue();
                    }

                    if (IsMagicWordDetected()) //theres probably a slightly more efficient way than checking each 8 bytes, but this is drops in the bucket
                    {
                        _magicWordQueue.Clear();

                        if (Status != State.Paused)
                            ProcessFrame();
                    }
                }
            }
            catch (Exception ex) when (ex is IOException ||
                        ex is TimeoutException ||
                        ex is InvalidOperationException ||
                        ex is UnauthorizedAccessException)
            {
                Close();
                Log.Error("Serial Connection Failed");
            }
        }


        private bool IsMagicWordDetected()
        {
            if (_magicWordQueue.Count < TLV_Constants.MAGIC_WORD.Length)
                return false;

            int i = 0;
            foreach (var value in _magicWordQueue)
            {
                if (value != TLV_Constants.MAGIC_WORD[i])
                    return false;
                i++;
            }
            return true;
        }

        private void ProcessFrame()
        {
            if (_dataPort == null || !_dataPort.IsOpen)
                return;

            try
            {
                var bytesForRestOfHeader = TLV_Constants.FULL_FRAME_HEADER_SIZE - TLV_Constants.MAGIC_WORD.Length; // 32
                var restOfHeader = new byte[bytesForRestOfHeader];

                int bytesRead = _dataPort.ReadExact(restOfHeader, 0, bytesForRestOfHeader);

                if (bytesRead < bytesForRestOfHeader)
                {
                    return;
                }

                FrameHeader? frameHeader;
                try
                {
                    frameHeader = FrameParser.CreateFrameHeader(restOfHeader);
                }
                catch(Exception e)
                {
                    Log.Error(e, "Frameheader failed to be parsed.");
                    return;
                }

                var nextBytesToRead = frameHeader.TotalPacketLength - TLV_Constants.FULL_FRAME_HEADER_SIZE;

                if (nextBytesToRead <= 0 || nextBytesToRead > 5000) //There should never be this much data in a single frame, if there is, throw it out
                {
                    return;
                }

                var tlvBuffer = new byte[nextBytesToRead];
                bytesRead = _dataPort.ReadExact(tlvBuffer, 0, tlvBuffer.Length);
                if (bytesRead < tlvBuffer.Length)
                {
                    return;
                }

                //serial thread doesn't worry about this anymore
                Task.Run(() => CreateAndNotifyFrame(tlvBuffer, frameHeader));
            }
            catch (Exception ex) when (ex is IOException ||
                            ex is TimeoutException ||
                            ex is InvalidOperationException ||
                            ex is UnauthorizedAccessException)
            {
                Close();
                Log.Error(ex, $"Dataport has failed.");
            }
        }


        //private object _startPollingLock = new();
        private Event? _old;
        private object _notifyFrameLock;
        //this method is very complicated... and for good reason.
        //basically target indices for a frame match to the points of n-1 frames. yes. it's a nightmare
        //so now we have to hold onto the "old" frame, and when a new one comes in:
        //firstly, we only notify with the old frame, new frame will become old frame as long as there isn't any funny business
        //if the new one is null (something parsing wise failed), we throw out the old one
        //if we have target indicies in the new frame, we need to match them to the old frames points, if the counts of both don't match, we throw out this new one and the old one
        //we then ship off the old one then old becomes the new one

        //therefore, every frame is sent on the next call of this function
        private void CreateAndNotifyFrame(Span<byte> tlvBuffer, FrameHeader frameHeader)
        {
            lock (_notifyFrameLock)
            {
                try
                {
                    var resultingEvent = FrameParser.CreateEvent(tlvBuffer, frameHeader);

                    if (resultingEvent == null)
                    {
                        _old = null; //we don't send this one if the last is null
                        Log.Error("Resultant frame is null");
                        return;
                    }

                    if (resultingEvent.TargetIndices != null)
                    {
                        if (_old?.Points?.Count != resultingEvent.TargetIndices.Count)
                        {
                            _old = null; //we don't send this one if the counts don't match
                            Log.Error("Frame target indices doesn't match expected number of points");
                            return;
                        }

                        for (int i = 0; i < resultingEvent.TargetIndices.Count; i++)
                        {
                            _old!.Points![i].TID = resultingEvent.TargetIndices[i];
                        }
                    }

                    if (_old != null)
                    {
                        //BIG TODO: put processing logic here
                    }

                    _old = resultingEvent;
                }
                catch (Exception e)
                {
                    _old = null;
                    Log.Error(e, "Bad Frame");
                }
            }
        }

        public State TryWriteConfigFromFile()
        {
            return TryWriteConfigToModule(FileHelper.FindAndReadConfigFile());
        }

        public State TryWriteConfigFromFile(string[] configStrings)
        {
            return TryWriteConfigToModule(configStrings);
        }

        //TODO: see if the module returns anything and base off of that, possibly wait and see if the data port has new data...?

        int _configLineSendTimeInMs = 10;
        private State TryWriteConfigToModule(string[] configStrings)
        {
            if (_cliPort == null || !_cliPort.IsOpen)
            {
                Close();
                Log.Error($"CLI port failed to connect.");
                return Status;
            }

            try
            {
                foreach (var line in configStrings)
                {
                    if (!line.Contains('%') && !string.IsNullOrEmpty(line) && !(line == "\n"))
                    {
                        string trimmedLine = line.Trim('\n');
                        _cliPort.WriteLine(trimmedLine);
                        Thread.Sleep(_configLineSendTimeInMs);
                        Console.WriteLine(trimmedLine);
                    }
                }
            }
            catch (Exception ex) when (ex is IOException ||
                            ex is TimeoutException ||
                            ex is InvalidOperationException ||
                            ex is UnauthorizedAccessException)
            {
                Close();
                Log.Error(ex, $"CLI Port: [{_cliPort.PortName}] failed to connect.");
            }

            return Status;
        }


        public enum State
        {
            AwaitingPortInit,       //We haven't started anything yet
            Running,                //Actively waiting on events
            Paused                  //We can start whenever
        }
    }
}
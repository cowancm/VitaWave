using System.Text;
using System.IO.Ports;
using System.Collections.Concurrent;
using Serilog;
using VitaWave.ModuleControl.Utils;
using VitaWave.ModuleControl.Parsing.TLVs;
using VitaWave.ModuleControl.Interfaces;

namespace VitaWave.ModuleControl.Parsing
{
    public sealed class ModuleIO : IModuleIO
    {
        public ModuleIO(IRuntimeSettingsManager settingsManager, ISerialProcessor serialDataProcessor)
        {
            _settingsManager = settingsManager;
            _serialDataProcessor = serialDataProcessor;

            ChangePortSettings();
            Status = State.AwaitingPortInit;
        }

        public event EventHandler? OnConnectionLost;
        public State Status { get; private set; }

        private string? _dataPortName;
        private string? _cliPortName;
        private int _dataBaud;
        private int _cliBaud;
        private SerialPort? _dataPort;
        private SerialPort? _cliPort;

        private readonly IRuntimeSettingsManager _settingsManager;
        private readonly ISerialProcessor _serialDataProcessor;

        const int MinimumBytesForOnDataRecieved = TLV_Constants.FULL_FRAME_HEADER_SIZE;
        const int DataBufferSizeInBytes = 16384;

        public void ChangePortSettings()
        {
            var settings = _settingsManager.GetSettings();

            if (settings != null)
            {
                _dataPortName = settings.DataPortName;
                _cliPortName = settings.CliPortName;
                _dataBaud = settings.DataBaud;
                _cliBaud = settings.CliBaud;
            }
            else
            {
                Log.Error("Serial port settings is null.");
            }
        }

        public State InitializePorts()
        {
            if (Status != State.AwaitingPortInit)
            {
                Stop();
            }

            try
            {
                _dataPort = new SerialPort(_dataPortName, _dataBaud, Parity.None, 8, StopBits.One)
                {
                    WriteTimeout = 5000,
                    ReadTimeout = 1000,
                    ReadBufferSize = DataBufferSizeInBytes
                };

                _cliPort = new SerialPort(_cliPortName, _cliBaud, Parity.None, 8, StopBits.One)
                {
                    WriteTimeout = 5000,
                    Encoding = Encoding.UTF8,
                };

                _dataPort.ReceivedBytesThreshold = MinimumBytesForOnDataRecieved;
                _dataPort.DataReceived += OnDataRecieved;

                _dataPort.Open();
                _cliPort.Open();
            }
            catch (Exception ex)
            {
                Stop();
                Log.Error(ex, $"Error Initializing ports." +
                            $"\nPorts: DATA[{_dataPortName ?? "NULL"}], CLI[{_cliPortName ?? "NULL"}]");
                return Status;
            }
            Status = State.Paused;
            return Status;
        }

        public State Run()
        {
            if(_dataPort == null || !_dataPort.IsOpen)
                return Status;

            Status = State.Running;
            return Status;
        }

        public void Pause()
        {
            Status = State.Paused; //This only pasuses sending/processing new frames, serial buffers are still getting read
        }

        public State Stop()
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


        ConcurrentQueue<int> _magicWordQueue = new ConcurrentQueue<int>();
        Lock _serialReadingLock = new();
        private void OnDataRecieved(object sender, SerialDataReceivedEventArgs e)
        {

            if(!_serialReadingLock.TryEnter())
                return;

            try
            {
                while (_dataPort?.BytesToRead > 0)
                {
                    int byteValue = _dataPort.ReadByte();
                    _magicWordQueue.Enqueue(byteValue);

                    if (_magicWordQueue.Count > TLV_Constants.MAGIC_WORD.Length)
                    {
                        _magicWordQueue.TryDequeue(out byteValue);
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
                Stop();
                Log.Error("Serial Connection Failed");
            }
            finally
            {
                _serialReadingLock.Exit();
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

                _serialDataProcessor.AddToQueue(tlvBuffer, frameHeader);
            }
            catch (Exception ex) when (ex is IOException ||
                            ex is TimeoutException ||
                            ex is InvalidOperationException ||
                            ex is UnauthorizedAccessException)
            {
                Stop();
                Log.Error(ex, $"Dataport has failed.");
            }
        }

        public async Task<bool> TryWriteConfigFromFile()
        {
            var currentDirectory = Directory.GetCurrentDirectory();
            var files = Directory.GetFiles(currentDirectory, "*.cfg");
            var configFile = files?.First();

            if (configFile == null)
            {
                Log.Error("No config file found when trying to write config file.");
                return false;
            }
            else if (files?.Length > 0)
            {
                Log.Error($"Multiple config files found, using \"{configFile}\"");
            }

            return await TryWriteConfigToModule(File.ReadAllLines(configFile));
        }

        public async Task<bool> TryWriteConfigFromFile(string[] configStrings)
        {
            return await TryWriteConfigToModule(configStrings);
        }

        int _configLineSendTimeInMs = 10;

        private async Task<bool> TryWriteConfigToModule(string[] configStrings)
        {
            if (_cliPort == null || !_cliPort.IsOpen)
            {
                Stop();
                Log.Error($"CLI port failed to connect.");
                return false;
            }

            try
            {
                foreach (var line in configStrings)
                {
                    if (!line.Contains('%') && !string.IsNullOrEmpty(line) && !(line == "\n"))
                    {
                        string trimmedLine = line.Trim('\n');
                        _cliPort.WriteLine(trimmedLine);
                        await Task.Delay(_configLineSendTimeInMs);  // Non-blocking delay
                        System.Console.WriteLine(trimmedLine);
                    }
                }
            }
            catch (Exception ex) when (ex is IOException ||
                            ex is TimeoutException ||
                            ex is InvalidOperationException ||
                            ex is UnauthorizedAccessException)
            {
                Stop();
                Log.Error(ex, $"CLI Port: [{_cliPort.PortName}] failed to connect.");
                return false;
            }

            return true;
        }
    }
}
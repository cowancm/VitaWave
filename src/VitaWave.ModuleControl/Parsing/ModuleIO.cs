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
        private Task? _pollSerialTask = null;

        private readonly IRuntimeSettingsManager _settingsManager;
        private readonly ISerialProcessor _serialDataProcessor;

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

        public State Run(CancellationToken ct)
        {
            if (_pollSerialTask == null)
            {
                _pollSerialTask = Task.Run(() => PollSerial(ct));
                Status = State.Running;
            }

            return Status;
        }

        public State Pause()
        {
            if (_pollSerialTask == null)
                return Status;

            Status = State.Paused;
            return Status; //This only pasuses sending/processing new frames, serial buffers are still getting read
        }

        object _stopLock = new();
        public State Stop()
        {
            lock(_stopLock)
            {
                Status = State.AwaitingPortInit;
                _pollSerialTask = null;
                OnConnectionLost?.Invoke(this, EventArgs.Empty);
                ClosePorts();
            }
            return Status;
        }

        private void ClosePorts()
        {

            try
            {
                if (_dataPort != null)
                {
                    if (_dataPort.IsOpen)
                    {
                        try { _dataPort.Close(); } catch { /* ignore errors during close */ } finally { _dataPort.Dispose(); }
                    }
                    _dataPort = null;
                }

                if (_cliPort != null)
                {
                    if (_cliPort.IsOpen)
                    {
                        try { _cliPort.Close(); } catch { /* ignore errors during close */ } finally { _cliPort.Dispose(); }
                    }
                    _cliPort = null;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error closing ports");
            }
        }


        private int[] _magicWordBuffer;
        private int _bufferIndex = 0;

        private async void PollSerial(CancellationToken ct)
        {
            // Initialize buffer to hold exactly the magic word length
            _magicWordBuffer = new int[TLV_Constants.MAGIC_WORD.Length];

            Thread.CurrentThread.Priority = ThreadPriority.Highest;

            try
            {
                while (!ct.IsCancellationRequested)
                {
                    while (_dataPort?.BytesToRead > TLV_Constants.MAGIC_WORD.Length && Status == State.Running)
                    {
                        int byteValue = _dataPort.ReadByte();

                        _magicWordBuffer[_bufferIndex] = byteValue;
                        _bufferIndex = (_bufferIndex + 1) % TLV_Constants.MAGIC_WORD.Length;

                        if (IsMagicWordDetected())
                        {
                            ProcessFrame();
                        }
                    }
                }
            }
            catch (Exception ex) when (ex is IOException ||
                        ex is TimeoutException ||
                        ex is InvalidOperationException ||
                        ex is UnauthorizedAccessException ||
                        ex is NullReferenceException)
            {
                Log.Error(ex, "Serial Connection Failed");
            }
            Stop();
        }

        private bool IsMagicWordDetected()
        {
            for (int i = 0; i < TLV_Constants.MAGIC_WORD.Length; i++)
            {
                int bufferPosition = (_bufferIndex + i) % TLV_Constants.MAGIC_WORD.Length;
                if (_magicWordBuffer[bufferPosition] != TLV_Constants.MAGIC_WORD[i])
                    return false;
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
            else if (files?.Length > 1)
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
                        Log.Information("Sending to CLI: " + trimmedLine);
                        _cliPort.WriteLine(trimmedLine);
                        await Task.Delay(_configLineSendTimeInMs);
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
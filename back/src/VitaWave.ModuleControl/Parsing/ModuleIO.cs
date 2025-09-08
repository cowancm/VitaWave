using System.Text;
using System.IO.Ports;
using Serilog;
using VitaWave.ModuleControl.Utils;
using VitaWave.ModuleControl.Parsing.TLVs;
using VitaWave.ModuleControl.Interfaces;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using VitaWave.ModuleControl.Settings;

namespace VitaWave.ModuleControl.Parsing
{
    public class ModuleIO : IModuleIO, INotifyPropertyChanged
    {
        public ModuleIO(ISerialProcessor serialDataProcessor)
        {
            _serialDataProcessor = serialDataProcessor;

            ChangePortSettings(); //init
            Status = State.AwaitingPortInit;
        }

        private State _status;
        public State Status
        {
            get => _status;
            set
            {
                if (_status != value)
                {
                    _status = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private string? _dataPortName;
        private string? _cliPortName;
        private int _dataBaud;
        private int _cliBaud;
        private SerialPort? _dataPort;
        private SerialPort? _cliPort;
        private Task? _pollSerialTask = null;

        private readonly ISerialProcessor _serialDataProcessor;

        const int DataBufferSizeInBytes = 16384;

        private void ChangePortSettings()
        {
            var settings = SettingsManager.GetConfigSettings();

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

            ChangePortSettings();

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

        CancellationTokenSource _cts = new CancellationTokenSource();

        public State Run()
        {

            if ((_pollSerialTask == null || _pollSerialTask == Task.CompletedTask) && Status != State.AwaitingPortInit)
            {
                var token = _cts.Token;
                _pollSerialTask = Task.Run(() => PollSerial(token));
                Status = State.Running;
            }

            if (Status == State.Paused)
                Status = State.Running;

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
        int _taskCompletionTimeoutInMs = 100;
        public State Stop()
        {
            lock(_stopLock)
            {
                _cts.Cancel();
                _pollSerialTask?.Wait(_taskCompletionTimeoutInMs);
                _cts.Dispose();
                _cts = new();
                Status = State.AwaitingPortInit;
                _pollSerialTask = null;
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


        private int[] _magicWordBuffer = new int[TLV_Constants.MAGIC_WORD.Length];
        private int _bufferIndex = 0;

        private void PollSerial(CancellationToken ct)
        {
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
            catch (Exception ex)
            {
                Log.Error(ex, "Serial Connection Failed");
                Stop();
            }
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
            catch (Exception ex)
            {
                Stop();
                Log.Error(ex, $"Dataport has failed.");
            }
        }

        int _configLineSendTimeInMs = 10;

        public async Task<bool> TryWriteConfigToModule()
        {
            if (_cliPort == null || !_cliPort.IsOpen)
            {
                Stop();
                Log.Error($"CLI port failed to connect.");
                return false;
            }

            try
            {
                var lines = File.ReadAllLines(SettingsManager.GetTIConfigPath());

                foreach (var line in lines)
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
            catch (Exception ex)
            {
                Stop();
                Log.Error(ex, "");
                return false;
            }
            return true;
        }
    }
}
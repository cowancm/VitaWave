using System.Text;
using System.IO.Ports;
using ModuleControl.Parsing;
using ModuleControl.Utils;
using Common.PreProcessed.TLVs;
using Common.Interfaces;

namespace ModuleControl.Communication
{
    public sealed class ModuleIO : IModuleIO
    {
        #region Singleton Implementation
        private static readonly Lazy<ModuleIO> _instance = new Lazy<ModuleIO>(() => new ModuleIO());
        public static ModuleIO Instance => _instance.Value;

        private ModuleIO()
        {
            _pollSerialTask = Task.CompletedTask;
        }
        #endregion

        #region Events and Properties
        public event EventHandler<FrameEventArgs>? OnFrameProcessed;
        public event EventHandler? OnConnectionLost;

        public bool IsRunning => _pollSerialTask.Status == TaskStatus.Running;
        public bool IsConnected => _dataPort?.IsOpen == true && _cliPort?.IsOpen == true;

        public string? DataPortName { get; private set; }
        public string? CliPortName { get; private set; }
        #endregion

        #region Fields
        private SerialPort? _dataPort;
        private SerialPort? _cliPort;
        private Task _pollSerialTask;
        private CancellationTokenSource? _pollCancellationSource;

        private const int DATA_BAUD_RATE = 921600;
        private const int CLI_BAUD_RATE = 115200;

        #endregion

        #region Initialization and Port Management

        /// <summary>
        /// Initializes ports. Closes everything if still running. Needs to be ran before starting (first or again).
        /// </summary>
        /// <param name="dataPortName"></param>
        /// <param name="cliPortName"></param>
        /// <exception cref="InvalidOperationException"></exception>
        public void InitializePorts(string dataPortName, string cliPortName)
        {
            Stop();

            DataPortName = dataPortName;
            CliPortName = cliPortName;

            if (string.IsNullOrEmpty(DataPortName) || string.IsNullOrEmpty(CliPortName))
                throw new ArgumentException("Port names must be set before initializing ports");

            _dataPort = new SerialPort(DataPortName, DATA_BAUD_RATE, Parity.None, 8, StopBits.One)
            {
                WriteTimeout = 5000,
                ReadTimeout = 1000,
                ReadBufferSize = 4096
            };

            _cliPort = new SerialPort(CliPortName, CLI_BAUD_RATE, Parity.None, 8, StopBits.One)
            {
                WriteTimeout = 5000,
                Encoding = Encoding.UTF8,
            };

            try
            {
                _dataPort.Open();
                _cliPort.Open();
            }
            catch (Exception ex)
            {
                ClosePorts();
                OnConnectionLost?.Invoke(this, EventArgs.Empty);
                throw new InvalidOperationException($"Failed to open serial ports: {ex.Message}", ex);
            }
        }

        public bool? TrySendConfig()
        {
            string[] configLines;

            try
            {
                var configFilePath = AppDomain.CurrentDomain.BaseDirectory; //Config files will just go in root
                var sampleFile = Directory.GetFiles(configFilePath, "*.cfg").Single();
                configLines = File.ReadAllLines(sampleFile);
            }
            catch 
            {
                return null;
            }

            return TryWriteConfigFile(configLines);
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
        #endregion

        #region Start and Stop
        public bool StartDataPolling()
        {
            if (_dataPort == null || !_dataPort.IsOpen)
                return false;

            if (IsRunning)
                return true;

            _pollCancellationSource = new CancellationTokenSource();
            _pollSerialTask = Task.Run(() => PollSerial(_pollCancellationSource.Token));

            return true;
        }

        public void Stop()
        {
            if (_pollCancellationSource != null && !_pollCancellationSource.IsCancellationRequested)
            {
                _pollCancellationSource.Cancel();
                try
                {
                    // Wait for the task to complete with a timeout
                    if (!_pollSerialTask.IsCompleted)
                    {
                        _pollSerialTask.Wait(TimeSpan.FromSeconds(1));
                    }
                }
                catch
                {
                    // Ignore exceptions during cancellation
                }
                finally
                {
                    _pollCancellationSource.Dispose();
                    _pollCancellationSource = null;
                }
            }
            ClosePorts();
        }
        #endregion

        #region Serial Communication
        private void PollSerial(CancellationToken cancellationToken)
        {
            var queue = new Queue<int>();

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    if (_dataPort!.BytesToRead >= 4)
                    {
                        int byteValue = _dataPort.ReadByte();
                        queue.Enqueue(byteValue);

                        if (queue.Count > TLV_Constants.MAGIC_WORD.Length)
                        {
                            queue.Dequeue();
                        }

                        if (IsMagicWordDetected(queue)) //theres probably a slightly more efficient way than checking each 8 bytes, but this is drops in the bucket
                        {
                            queue.Clear();
                            ProcessFrame();
                        }
                    }
                    else
                    {
                        Thread.Sleep(2);
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex) when (ex is IOException ||
                            ex is TimeoutException ||
                            ex is InvalidOperationException ||
                            ex is UnauthorizedAccessException)
                {
                    OnConnectionLost?.Invoke(this, EventArgs.Empty);
                    ClosePorts();
                    break;
                }
            }
        }

        private bool IsMagicWordDetected(Queue<int> possibleMagicWord)
        {
            if (possibleMagicWord.Count < TLV_Constants.MAGIC_WORD.Length)
                return false;

            int i = 0;
            foreach (var value in possibleMagicWord)
            {
                if (value != TLV_Constants.MAGIC_WORD[i])
                    return false;
                i++;
            }
            return true;
        }

        private void ProcessFrame()
        {
            Thread.CurrentThread.Priority = ThreadPriority.Highest;

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

                var frameHeader = FrameParser.CreateFrameHeader(restOfHeader);
                var nextBytesToRead = frameHeader.TotalPacketLength - TLV_Constants.FULL_FRAME_HEADER_SIZE;

                if (nextBytesToRead <= 0 || nextBytesToRead > 50000) //There should never be this much data in a single frame, if there is, throw it out
                {
                    return;
                }

                var tlvBuffer = new byte[nextBytesToRead];
                bytesRead = _dataPort.ReadExact(tlvBuffer, 0, tlvBuffer.Length);
                if (bytesRead < tlvBuffer.Length)
                {
                    return;
                }

                Task.Run(() => CreateAndNotifyFrame(tlvBuffer, frameHeader));
                //CreateAndNotifyFrame(tlvBuffer, frameHeader); For debug.
            }
            catch (Exception)
            {
                // Log
            }
        }

        private void CreateAndNotifyFrame(Span<byte> tlvBuffer, FrameHeader frameHeader)
        {
            try
            {
                var resultingEvent = FrameParser.CreateEvent(tlvBuffer, frameHeader);

                if (resultingEvent != null)
                {
                    OnFrameProcessed?.Invoke(this, new FrameEventArgs(resultingEvent));
                }
            }
            catch (Exception)
            {
                // Log
            }
        }
        #endregion

        #region CLI Communication

        public bool TryWriteConfig()
        {
            return TryWriteConfigFile(FileHelper.FindAndReadConfigFile());
        }

        private bool TryWriteConfigFile(string[] configString)
        {
            if (_cliPort == null || !_cliPort.IsOpen)
                return false;

            try
            {
                foreach (var line in configString)
                {
                    if (!line.Contains('%') && !string.IsNullOrEmpty(line) && !(line == "\n"))
                    {
                        string trimmedLine = line.Trim('\n');
                        _cliPort.WriteLine(trimmedLine);
                        Thread.Sleep(10);
                        Console.WriteLine(trimmedLine);
                    }
                }
                return true;
            }
            catch (Exception ex) when (ex is IOException ||
                            ex is TimeoutException ||
                            ex is InvalidOperationException ||
                            ex is UnauthorizedAccessException)
            {
                OnConnectionLost?.Invoke(this, EventArgs.Empty);
                ClosePorts();
                return false;
            }
        }

        #endregion
    }
}
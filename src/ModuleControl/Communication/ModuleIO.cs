using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;
using ModuleControl.Parsing;
using ModuleControl.Parsing.TLVs;
using System.Text;
using System.IO;

namespace ModuleControl.Communication
{
    public sealed class ModuleIO : IDisposable
    {
        #region Singleton Implementation
        private static readonly Lazy<ModuleIO> _instance = new Lazy<ModuleIO>(() => new ModuleIO());

        public static ModuleIO Instance => _instance.Value;

        // Private constructor ensures singleton pattern
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
        private bool _disposed = false;
        private Task _pollSerialTask;
        private CancellationTokenSource? _pollCancellationSource;

        private static readonly int[] MAGIC_WORD = { 2, 1, 4, 3, 6, 5, 8, 7 };
        private const int DATA_BAUD_RATE = 921600;
        private const int CLI_BAUD_RATE = 115200;
        private const int FULL_HEADER_SIZE = 40;
        #endregion

        #region Initialization and Port Management
        public void Initialize(string dataPortName, string cliPortName)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ModuleIO));

            Stop();

            DataPortName = dataPortName;
            CliPortName = cliPortName;

            InitPorts();
        }

        public void InitPorts()
        {
            ClosePorts(); //close anything that could be open

            if (string.IsNullOrEmpty(DataPortName) || string.IsNullOrEmpty(CliPortName))
                throw new InvalidOperationException("Port names must be set before initializing ports");

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
                _disposed = false;
            }
            catch (Exception ex)
            {
                ClosePorts();
                throw new InvalidOperationException($"Failed to open serial ports: {ex.Message}", ex);
            }
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
        public bool Start()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ModuleIO));

            if (_dataPort == null || !_dataPort.IsOpen || _cliPort == null || !_cliPort.IsOpen)
                return false;

            if (IsRunning)
                return true; // Already running

            // Create a new cancellation token source
            _pollCancellationSource = new CancellationTokenSource();

            // Start the polling task
            _pollSerialTask = Task.Run(() => PollSerial(_pollCancellationSource.Token));

            return true;
        }

        public void Stop()
        {
            // Cancel the polling task first
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

            try
            {
                while (!cancellationToken.IsCancellationRequested && _dataPort != null && _dataPort.IsOpen)
                {
                    try
                    {
                        if (_dataPort.BytesToRead > 0)
                        {
                            int byteValue = _dataPort.ReadByte();
                            queue.Enqueue(byteValue);

                            if (queue.Count > MAGIC_WORD.Length)
                            {
                                queue.Dequeue();
                            }

                            if (IsMagicWordDetected(queue))
                            {
                                queue.Clear();
                                ProcessFrame();
                            }
                        }
                        else
                        {
                            Thread.Sleep(5);
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
            finally
            {
                // If we exited due to an error (not cancellation), make sure we notify
                if (!cancellationToken.IsCancellationRequested && !_disposed)
                {
                    OnConnectionLost?.Invoke(this, EventArgs.Empty);
                    ClosePorts();
                }
            }
        }

        private bool IsMagicWordDetected(Queue<int> possibleMagicWord)
        {
            if (possibleMagicWord.Count < MAGIC_WORD.Length)
                return false;

            int i = 0;
            foreach (var value in possibleMagicWord)
            {
                if (value != MAGIC_WORD[i])
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
                var bytesForRestOfHeader = FULL_HEADER_SIZE - MAGIC_WORD.Length; // 32
                var restOfHeader = new byte[bytesForRestOfHeader];

                int bytesRead = _dataPort.Read(restOfHeader, 0, bytesForRestOfHeader);
                if (bytesRead < bytesForRestOfHeader)
                {
                    return;
                }

                var frameHeader = FrameParser.CreateFrameHeader(restOfHeader);
                var nextBytesToRead = frameHeader.TotalPacketLength - FULL_HEADER_SIZE;

                if (nextBytesToRead <= 0 || nextBytesToRead > 1024 * 1024) //safety
                {
                    return;
                }

                var tlvBuffer = new byte[nextBytesToRead];
                bytesRead = _dataPort.Read(tlvBuffer, 0, tlvBuffer.Length);
                if (bytesRead < tlvBuffer.Length)
                {
                    return;
                }

                Task.Run(() => CreateAndNotifyFrame(tlvBuffer, frameHeader));
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
        public bool TryWriteConfigFile(string[] configString)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ModuleIO));

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
                        Thread.Sleep(10); // Small delay between writes

                        // Consider using a logger instead of Console
                        Console.WriteLine($"Sent to CLI: {trimmedLine}");
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
                ClosePorts(); // Close ports on error
                return false;
            }
        }

        #endregion

        #region IDisposable Implementation
        public void Dispose()
        {
            if (_disposed)
                return;

            Stop(); // Stop will also close ports
            _disposed = true;
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
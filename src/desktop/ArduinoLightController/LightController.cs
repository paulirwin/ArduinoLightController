using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ArduinoLightController
{
    public class LightController
    {
        public enum LightStatus : byte
        {
            On = 1,
            Off = 2
        }

        public enum Commands : byte
        {
            TurnOn = 1,
            TurnOff = 2
        }

        private const string MAGIC_PREFIX = "f347ur323";

        private SerialPort _serial;
        private string _portName = "COM1";
        private Commands? _command;

        private CancellationTokenSource _cancelTokenSource;
        private CancellationToken _cancelToken;

        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);

        public event EventHandler<bool> ConnectionChanged;
        public event EventHandler<Exception> ConnectFailed;
        public event EventHandler<LightStatus> StatusUpdate;

        public bool IsConnected
        {
            get
            {
                return ReadLock(() =>
                {
                    return _serial != null && _serial.IsOpen;
                });
            }
        }

        public string PortName
        {
            get { return ReadLock(() => _portName); }
            set
            {
                WriteLock(() =>
                {
                    _portName = value;
                });
            }
        }

        public void Connect()
        {
            bool isConnected = ReadLock(() => _serial != null);

            if (isConnected)
            {
                Trace.TraceInformation("Already connected, nothing to do.");
                return;
            }

            Trace.TraceInformation("Kicking off thread to connect...");
            ThreadPool.QueueUserWorkItem(ConnectInternal);
        }

        public void Disconnect()
        {
            bool isDisconnected = ReadLock(() => _serial == null);

            if (isDisconnected)
            {
                Trace.TraceInformation("Already disconnected, nothing to do.");
                return;
            }

            Trace.TraceInformation("Requesting cancellation of looper...");
            _cancelTokenSource.Cancel();
        }

        public void SendCommand(Commands command)
        {
            Trace.TraceInformation($"Enqueueing command: {command}");

            WriteLock(() =>
            {
                _command = command;
            });
        }

        private void ConnectInternal(object state)
        {
            WriteLock(() =>
            {
                try
                {
                    _serial = new SerialPort(_portName, 9600);
                    _serial.Open();
                    _cancelTokenSource = new CancellationTokenSource();
                    _cancelToken = _cancelTokenSource.Token;
                    ConnectionChanged?.Invoke(this, true);
                    Trace.TraceInformation($"Connected to {_portName}, queueing looper.");
                    ThreadPool.QueueUserWorkItem(Looper);
                }
                catch (Exception ex)
                {
                    Trace.TraceError($"Error connecting: {ex.GetType().Name} - {ex.Message}");
                    _serial = null;
                    ConnectionChanged?.Invoke(this, false);
                    ConnectFailed?.Invoke(this, ex);
                }
            });
        }

        private void Looper(object state)
        {
            while (true)
            {
                bool isCanceled = ReadLock(() => _cancelToken.IsCancellationRequested);

                if (isCanceled)
                {
                    try
                    {
                        Trace.TraceInformation("Cancellation requested, closing port.");
                        WriteLock(() =>
                        {
                            _serial.Dispose();
                            _serial = null;
                            ConnectionChanged?.Invoke(this, false);
                        });
                    }
                    catch (Exception ex)
                    {
                        Trace.TraceError($"Error closing port: {ex.GetType().Name} - {ex.Message}");
                    }

                    return;
                }

                Commands? cmd = ReadLock(() => _command);

                if (cmd.HasValue)
                {
                    WriteLock(() =>
                    {
                        Trace.TraceInformation($"Sending command {cmd.Value}.");
                        byte[] command = new byte[MAGIC_PREFIX.Length + 2];
                        Encoding.ASCII.GetBytes(MAGIC_PREFIX).CopyTo(command, 0);
                        command[command.Length - 1] = (byte)cmd.Value;
                        _serial.Write(command, 0, command.Length);
                        string response = _serial.ReadTo("\n");

                        Trace.TraceInformation($"Command response: {response}");

                        if (response == "403")
                        {
                            // TODO: handle invalid prefix 
                            Trace.TraceWarning("403 received: invalid magic prefix!");
                        }

                        _command = null;
                        Trace.TraceInformation("Clearing current command.");
                    });

                    continue;
                }

                var status = (LightStatus)_serial.ReadByte();

                Trace.TraceInformation($"Status received: {status}");
                StatusUpdate?.Invoke(this, status);

                Thread.Sleep(500);
            }
        }

        private T ReadLock<T>(Func<T> func)
        {
            try
            {
                _lock.EnterReadLock();
                return func();
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        private void WriteLock(Action action)
        {
            try
            {
                _lock.EnterWriteLock();
                action();
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }
    }
}

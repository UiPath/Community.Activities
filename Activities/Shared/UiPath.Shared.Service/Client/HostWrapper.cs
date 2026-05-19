using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Diagnostics;
using System.Threading;

namespace UiPath.Shared.Service.Client
{
    internal class HostWrapper : IDisposable
    {
        private const int MaxBufferedLines = 100;

        private bool _disposed;

        private readonly object _disposeLock = new object();

        private readonly object _outputLock = new object();
        private readonly object _errorLock = new object();

        private readonly Queue<string> _stdoutBuffer = new Queue<string>(MaxBufferedLines + 1);
        private readonly Queue<string> _stderrBuffer = new Queue<string>(MaxBufferedLines + 1);

        private int _logTrace = 0;
        private int _initialized = 0;

        /// <summary>
        /// When true, output and error lines received after initialization are forwarded to Trace.
        /// </summary>
        internal bool LogTrace
        {
            get => Interlocked.CompareExchange(ref _logTrace, 0, 0) == 1;
            set => Interlocked.Exchange(ref _logTrace, value ? 1 : 0);
        }

        /// <summary>
        /// Set to true by Controller once the service is ready.
        /// Switches event handlers from buffering mode to trace-logging mode.
        /// </summary>
        internal bool Initialized
        {
            get => Interlocked.CompareExchange(ref _initialized, 0, 0) == 1;
            set => Interlocked.Exchange(ref _initialized, value ? 1 : 0);
        }

        internal Process Proc { get; set; }

        internal NamedPipeClientStream Pipe { get; set; }

        internal void AppendStdout(string line)
        {
            lock (_outputLock)
            {
                _stdoutBuffer.Enqueue(line);
                if (_stdoutBuffer.Count > MaxBufferedLines)
                    _stdoutBuffer.Dequeue();
            }
        }

        internal void AppendStderr(string line)
        {
            lock (_errorLock)
            {
                _stderrBuffer.Enqueue(line);
                if (_stderrBuffer.Count > MaxBufferedLines)
                    _stderrBuffer.Dequeue();
            }
        }

        public void Dispose()
        {
            lock (_disposeLock)
            {
                if (_disposed)
                    return;
                _disposed = true;
            }

            try
            {
                Pipe?.Dispose();
                if (Proc != null)
                {
                    if (!HostProcessHasExited())
                    {
                        Proc.Kill(entireProcessTree: true);
                        if (!Proc.WaitForExit(5000))
                            Trace.TraceWarning($"Host process {Proc.Id} did not exit within 5 seconds after Kill.");
                    }
                    Proc.Dispose();
                }
            }
            catch
            {
                //ignore exceptions on dispose, we don't care if the process is already killed or if the pipe is already closed
            }
            Pipe = null;
            Proc = null;

            lock (_outputLock)
                _stdoutBuffer.Clear();
            lock (_errorLock)
                _stderrBuffer.Clear();
        }

        internal bool HostProcessHasExited()
        {
            try
            {
                return Proc?.HasExited ?? true;
            }
            catch
            {
                //For wathever reason if HasExited throws, we assume that the process has not exited
                //Error will be thrown when timeout expires
                return false;
            }
        }

        internal bool GetHostProcessId(out int processId)
        {
            processId = 0;
            try
            {
                processId = Proc.Id;
                return processId > 0;
            }
            catch
            {
                //If the id is not set, it will throw
                return false;
            }
        }

        internal void ThrowIfProcessHasExited()
        {
            if (Proc != null && HostProcessHasExited())
            {
                // Drain any remaining lines still buffered in the async readers
                Proc.WaitForExit();

                string output;
                string error;

                lock (_outputLock)
                    output = string.Join(Environment.NewLine, _stdoutBuffer);
                lock (_errorLock)
                    error = string.Join(Environment.NewLine, _stderrBuffer);

                throw new Exception($"Host process has exited!\n latest output: {output} \n latest error: {error} \n");
            }
        }
    }
}

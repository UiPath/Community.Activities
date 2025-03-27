using System;
using System.IO.Pipes;
using System.Diagnostics;

namespace UiPath.Shared.Service.Client
{
    internal class HostWrapper : IDisposable
    {
        private bool _disposed;

        private readonly object _disposeLock = new object();

        internal Process Proc { get; set; }

        internal NamedPipeClientStream Pipe { get; set; }

        public void Dispose()
        {
            lock (_disposeLock)
            {
                if (_disposed)
                    return;
                _disposed = true;
            }

            Pipe?.Dispose();
            Pipe = null;

            //Prevent process leak in certain error scenarios
            Proc?.Kill();
            Proc = null;
        }

        internal bool HostProcessHasExited()
        {
            try
            {
                return Proc.HasExited;
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
            if (HostProcessHasExited())
            {
                using (var readerOutput = Proc.StandardOutput)
                using (var readerError = Proc.StandardError)
                {
                    string output = readerOutput.ReadToEnd();
                    string error = readerError.ReadToEnd();
                    throw new Exception($"Host process has exited!\n output: {output} \n error: {error} \n");
                }
            }
        }
    }
}

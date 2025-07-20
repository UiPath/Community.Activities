using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Reflection;
using System.Threading;
using System.Runtime.InteropServices;

namespace UiPath.Shared.Service.Client
{
    internal class Controller<T>
    {
        /// <summary>
        /// time between retries for service availability
        /// </summary>
        private readonly TimeSpan RetryInterval = TimeSpan.FromMilliseconds(500);

        //Timeout in milliseconds for pipe connection (attempt)
        private readonly int PipeConnectionTimeoutMs = 1000;

        internal HostWrapper PythonWrapper = new HostWrapper();

        internal bool Visible { get; set; } = true;

        internal string HostLibFile { get; set; }

        internal TimeSpan StartTimeout { get; set; } = Config.DefaultServiceCreationTimeout;

        internal HostWrapper Create()
        {
            StartHostService();
            return PythonWrapper;
        }

        internal void ForceStop()
        {
            PythonWrapper?.Proc.Kill();
        }

        private void StartHostService()
        {
            var isWindows = true;
#if NETCOREAPP
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                isWindows = false;
#endif
            string folder = Path.GetDirectoryName(HostLibFile);
            var hostLibFullPath = HostLibFile;
            if (folder.IsNullOrEmpty())
            {
                if (isWindows)
                    folder = Path.GetDirectoryName(Assembly.GetAssembly(typeof(T)).Location).Replace("\\lib\\", "\\bin\\");
                else
                    folder = Path.GetDirectoryName(Assembly.GetAssembly(typeof(T)).Location).Replace("/lib/", "/bin/");

                hostLibFullPath = Path.Combine(folder, HostLibFile);
            }

            if (!File.Exists(hostLibFullPath))
                throw new Exception($"Process path not found: {hostLibFullPath}");

            // start the host process using dotnet
            ProcessStartInfo psi = new ProcessStartInfo()
            {
                UseShellExecute = false,
                FileName = "dotnet",
                WorkingDirectory = folder,
                WindowStyle = Visible ? ProcessWindowStyle.Normal : ProcessWindowStyle.Hidden,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };
            psi.ArgumentList.Add(hostLibFullPath);

            PythonWrapper.Proc = Process.Start(psi);

            Retry(ServiceReady, StartTimeout, RetryInterval);

            // wait for service to become available
            bool ServiceReady()
            {
                PythonWrapper.ThrowIfProcessHasExited();

                //for some edge case - check if the process has the id set               
                if (!PythonWrapper.GetHostProcessId(out var processId))
                    return false;

                PythonWrapper.Pipe ??= new NamedPipeClientStream(".", processId.ToString(), PipeDirection.InOut, PipeOptions.Asynchronous);

                TryConnectPipeClient();
                return PythonWrapper.Pipe.IsConnected;
            }

            void TryConnectPipeClient()
            {
                try
                {
                    PythonWrapper.Pipe.Connect(PipeConnectionTimeoutMs);
                }
                catch
                {
                    //In case of exception we are going to retry to connect next time
                    //On timeout, if failure persists exception will be thrown
                }
            }
        }

        private static void Retry(Func<bool> checkFunction, TimeSpan timeout, TimeSpan retryInterval)
        {
            Stopwatch sw = Stopwatch.StartNew();
            while (!checkFunction())
            {
                Thread.Sleep(retryInterval);
                if (sw.Elapsed > timeout)
                {
                    Trace.TraceError($"Waiting for service start reached timeout ({timeout})");
                    throw new TimeoutException($"Error waiting for host service. Timeout: {timeout}");
                }
            }
        }
    }
}
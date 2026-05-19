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

        internal string PythonHostLibFile { get; set; }
        
        internal string PythonHostExeFile { get; set; }

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
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                isWindows = false;

            bool isExeMode = !PythonHostExeFile.IsNullOrEmpty();
            string hostFile = isExeMode ? PythonHostExeFile : PythonHostLibFile;

            string folder = Path.GetDirectoryName(hostFile);
            var hostFullPath = hostFile;
            if (folder.IsNullOrEmpty())
            {
                if (isWindows)
                    folder = Path.GetDirectoryName(Assembly.GetAssembly(typeof(T)).Location).Replace("\\lib\\", "\\bin\\");
                else
                    folder = Path.GetDirectoryName(Assembly.GetAssembly(typeof(T)).Location).Replace("/lib/", "/bin/");

                hostFullPath = Path.Combine(folder, hostFile);
            }

            if (!File.Exists(hostFullPath))
                throw new Exception($"Process path not found: {hostFullPath}");

            // start the host process: directly via exe on x86, or via dotnet for the managed lib
            ProcessStartInfo psi = new ProcessStartInfo()
            {
                UseShellExecute = false,
                FileName = isExeMode ? hostFullPath : "dotnet",
                WorkingDirectory = folder,
                WindowStyle = Visible ? ProcessWindowStyle.Normal : ProcessWindowStyle.Hidden,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };
            if (!isExeMode)
                psi.ArgumentList.Add(hostFullPath);

            PythonWrapper.Proc = Process.Start(psi);

            PythonWrapper.Proc.OutputDataReceived += (s, e) =>
            {
                if (e.Data == null) return;
                if (PythonWrapper.Initialized)
                {
                    if (PythonWrapper.LogTrace)
                        Trace.TraceInformation($"[python.stdout] {e.Data}");
                }
                else
                {
                    PythonWrapper.AppendStdout(e.Data);
                }
            };
            PythonWrapper.Proc.ErrorDataReceived += (s, e) =>
            {
                if (e.Data == null) return;
                if (PythonWrapper.Initialized)
                {
                    if (PythonWrapper.LogTrace)
                        Trace.TraceWarning($"[python.stderr] {e.Data}");
                }
                else
                {
                    PythonWrapper.AppendStderr(e.Data);
                }
            };
            PythonWrapper.Proc.BeginOutputReadLine();
            PythonWrapper.Proc.BeginErrorReadLine();
            // Note: event handlers must be subscribed before BeginOutputReadLine/BeginErrorReadLine,
            // but both require the process to already be started — subscriptions cannot move before Process.Start.

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

                if (PythonWrapper.Pipe.IsConnected)
                {
                    PythonWrapper.Initialized = true;
                    return true;
                }
                return false;
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
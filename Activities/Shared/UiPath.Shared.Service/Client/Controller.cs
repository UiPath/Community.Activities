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
            bool isExeMode = !PythonHostExeFile.IsNullOrEmpty();
            string hostFile = isExeMode ? PythonHostExeFile : PythonHostLibFile;
            var (folder, hostFullPath) = ResolveHostFullPath(hostFile);

            if (!File.Exists(hostFullPath))
                throw new Exception($"Process path not found: {hostFullPath}");

            PythonWrapper.Proc = Process.Start(CreateProcessStartInfo(hostFullPath, folder, isExeMode));

            // Note: event handlers must be subscribed before BeginOutputReadLine/BeginErrorReadLine,
            // but both require the process to already be started — subscriptions cannot move before Process.Start.
            PythonWrapper.Proc.OutputDataReceived += (_, e) => RelayHostOutput(e.Data, isStderr: false);
            PythonWrapper.Proc.ErrorDataReceived += (_, e) => RelayHostOutput(e.Data, isStderr: true);
            PythonWrapper.Proc.BeginOutputReadLine();
            PythonWrapper.Proc.BeginErrorReadLine();

            Retry(IsServiceReady, StartTimeout, RetryInterval);
        }

        private static (string folder, string hostFullPath) ResolveHostFullPath(string hostFile)
        {
            var folder = Path.GetDirectoryName(hostFile);
            if (!folder.IsNullOrEmpty())
                return (folder, hostFile);

            var assemblyDir = Path.GetDirectoryName(Assembly.GetAssembly(typeof(T)).Location);
            var resolvedFolder = IsWindows()
                ? assemblyDir.Replace("\\lib\\", "\\bin\\")
                : assemblyDir.Replace("/lib/", "/bin/");
            return (resolvedFolder, Path.Combine(resolvedFolder, hostFile));
        }

        private ProcessStartInfo CreateProcessStartInfo(string hostFullPath, string folder, bool isExeMode)
        {
            // start the host process: directly via exe on x86, or via dotnet for the managed lib
            var psi = new ProcessStartInfo
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
            return psi;
        }

        private void RelayHostOutput(string data, bool isStderr)
        {
            if (data == null)
                return;

            // Always buffer so ThrowIfProcessHasExited can surface the latest lines from the
            // host, regardless of whether the crash happens during startup or steady-state.
            if (isStderr)
                PythonWrapper.AppendStderr(data);
            else
                PythonWrapper.AppendStdout(data);

            // Trace-forward only after the service is initialized and tracing is enabled.
            if (!PythonWrapper.Initialized || !PythonWrapper.LogTrace)
                return;

            if (isStderr)
                Trace.TraceWarning($"[python.stderr] {data}");
            else
                Trace.TraceInformation($"[python.stdout] {data}");
        }

        // wait for service to become available
        private bool IsServiceReady()
        {
            PythonWrapper.ThrowIfProcessHasExited();

            //for some edge case - check if the process has the id set
            if (!PythonWrapper.GetHostProcessId(out var processId))
                return false;

            PythonWrapper.Pipe ??= new NamedPipeClientStream(".", processId.ToString(), PipeDirection.InOut, PipeOptions.Asynchronous);
            TryConnectPipeClient();

            if (!PythonWrapper.Pipe.IsConnected)
                return false;

            PythonWrapper.Initialized = true;
            return true;
        }

        private void TryConnectPipeClient()
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

        private static bool IsWindows() => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

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

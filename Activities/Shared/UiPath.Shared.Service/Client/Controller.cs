using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;

namespace UiPath.Shared.Service.Client
{
    internal class Controller<T>
    {
        /// <summary>
        /// time between retries for service availability
        /// </summary>
        private readonly TimeSpan RetryInterval = TimeSpan.FromMilliseconds(50);

        //Timeout in milliseconds for pipe connection (attempt)
        private readonly int PipeConnectionTimeoutMs = 1000;

        internal int ProcessId { get; private set; }

        internal string Arguments { get; set; } = null;

        internal bool Visible { get; set; } = true;

        internal string ExeFile { get; set; }

        internal NamedPipeClientStream Client { get; private set; }

        internal TimeSpan StartTimeout { get; set; } = Config.DefaultServiceCreationTimeout;

        internal NamedPipeClientStream pipeClient { get; set; }

        internal NamedPipeClientStream Create()
        {
            Client = StartHostService();
            return Client;
        }

        internal void ForceStop()
        {
            Process.GetProcessById(ProcessId)?.Kill();
        }

        private NamedPipeClientStream StartHostService()
        {
            var isWindows = true;
#if NETCOREAPP
            if(!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                isWindows = false;
#endif
            string folder = Path.GetDirectoryName(ExeFile);
            string exeFullPath = ExeFile;
            if (folder.IsNullOrEmpty())
            {
                if (isWindows)
                {
                    folder = Path.GetDirectoryName(Assembly.GetAssembly(typeof(T)).Location).Replace("\\lib\\", "\\bin\\");
                    exeFullPath = Path.Combine(folder, ExeFile);
                }
                else
                {
                    folder = Path.GetDirectoryName(Assembly.GetAssembly(typeof(T)).Location).Replace("/lib/", "/bin/");
                    Arguments = string.Concat(Path.Combine(folder, ExeFile.Replace(".exe", ".dll")), " ", Arguments);
                    exeFullPath = "dotnet";
                }


            }

            if (!File.Exists(exeFullPath) && isWindows
                || !isWindows && string.IsNullOrEmpty(Arguments))
                throw new Exception($"Process path not found: {exeFullPath}");

            // start the host process
            ProcessStartInfo psi = new ProcessStartInfo()
            {
                UseShellExecute = false,
                FileName = exeFullPath,
                WorkingDirectory = folder,
                Arguments = Arguments,
                WindowStyle = Visible ? ProcessWindowStyle.Normal : ProcessWindowStyle.Hidden,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };
            Process process = Process.Start(psi);

            // wait for service to become available
            bool ServiceReady()
            {
                if (HostProcessHasExited())
                {
                    using (var readerOutput = process.StandardOutput)
                    using (var readerError = process.StandardError)
                    {
                        string output = readerOutput.ReadToEnd();
                        string error = readerError.ReadToEnd();
                        throw new Exception($"Host process has exit!\n output: {output} \n error: {error} \n");
                    }
                }

                if (pipeClient == null)
                {
                    pipeClient = new NamedPipeClientStream(".", process.Id.ToString(), PipeDirection.InOut, PipeOptions.Asynchronous);
                }

                TryConnectPipeClient();
                if (pipeClient.IsConnected)
                {
                    return true;
                }
                return false;
            }

            void TryConnectPipeClient()
            {
                try
                {
                    pipeClient.Connect(PipeConnectionTimeoutMs);
                }
                catch
                {
                    //In case of exception we are going to retry to connect next time
                    //On timeout, if failure persists exception will be thrown
                }
            }

            bool HostProcessHasExited()
            {
                try
                {
                    return process.HasExited;
                }
                catch
                {
                    //For wathever reason if HasExited throws, we assume that the process has not exited
                    //Error will be thrown when timeout expires
                    return false;
                }
            }

            Retry(ServiceReady, StartTimeout, RetryInterval);
            return pipeClient;
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
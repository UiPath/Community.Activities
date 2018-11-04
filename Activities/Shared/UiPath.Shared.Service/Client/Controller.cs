using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;

namespace UiPath.Shared.Service.Client
{
    internal class Controller<T>
    {
        /// <summary>
        /// time between retries for service availability
        /// </summary>
        private readonly TimeSpan RetryInterval = TimeSpan.FromMilliseconds(50);

        internal int ProcessId { get; private set; }

        internal string Arguments { get; set; } = null;

        internal bool Visible { get; set; } = false;

        internal string ExeFile { get; set; }

        internal string Endpoint { get; private set; }

        internal TimeSpan StartTimeout { get; set; } = Config.DefaultServiceCreationTimeout;

        internal bool Create()
        {
            Endpoint = StartHostService();
            return !Endpoint.IsNullOrEmpty();
        }

        internal void ForceStop()
        {
            Process.GetProcessById(ProcessId)?.Kill();
        }

        private string StartHostService()
        {
            string folder = Path.GetDirectoryName(ExeFile);
            string exeFullPath = ExeFile;
            if (folder.IsNullOrEmpty())
            {
                folder = Path.GetDirectoryName(Assembly.GetAssembly(typeof(T)).Location);
                exeFullPath = Path.Combine(folder, ExeFile);
            }

            if (!File.Exists(exeFullPath))
                throw new Exception($"Process path not found: {exeFullPath}");

            // start the host process 
            ProcessStartInfo psi = new ProcessStartInfo()
            {
                FileName = exeFullPath,
                WorkingDirectory = folder,
                Arguments = Arguments,
                WindowStyle = Visible ? ProcessWindowStyle.Normal : ProcessWindowStyle.Hidden
            };
            Process process = Process.Start(psi);

            // endpoint name
            string endpoint = Config.MakeServiceAddress(typeof(T), process.Id);

            // wait for service to become available; using the endpoint name for mutex
            bool ServiceReady()
            {
                if (Mutex.TryOpenExisting(endpoint, out Mutex mutex))
                {
                    mutex.Close();
                    return true;
                }
                return false;
            }
            Retry(ServiceReady, StartTimeout, RetryInterval);
            return endpoint;
        }

        private static void Retry(Func<bool> checkFunction, TimeSpan timeout, TimeSpan retryInterval)
        {
            Stopwatch sw = Stopwatch.StartNew();
            while(!checkFunction())
            {
                Thread.Sleep(retryInterval);
                if(sw.Elapsed > timeout)
                {
                    Trace.TraceError($"Waiting for service start reached timeout ({timeout})");
                    throw new TimeoutException($"Error waiting for host service. Timeout: {timeout}");
                }
            }
        }
    }
}

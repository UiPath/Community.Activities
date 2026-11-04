using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace UiPath.Python.Host
{
    internal static class Program
    {
        private static PythonService _service = null;
        private static readonly CancellationTokenSource _cts = new CancellationTokenSource();

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main(string[] args)
        {
            // The resolver must be in place before any code that touches the assemblies it provides is
            // compiled, so everything else runs in a separate method.
            if (!HostAssemblyResolver.TryRegisterOrReport(args, Console.Error))
            {
                Environment.ExitCode = 2;
                return;
            }

            Run();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void Run()
        {
            AppDomain.CurrentDomain.ProcessExit += Application_ApplicationExit;

            _service = new PythonService();
            var serverTask = _service.RunServer(_cts.Token);

            try
            {
                // Block until the server stops. If RunServer throws an unexpected exception it will
                // propagate here and terminate the host process, which is preferable to leaving a
                // live-but-non-serving process that the client stays connected to indefinitely.
                serverTask.GetAwaiter().GetResult();
            }
            finally
            {
                // Unhook before disposing so a late ProcessExit callback can't touch a disposed CTS.
                AppDomain.CurrentDomain.ProcessExit -= Application_ApplicationExit;
                _cts.Dispose();
            }
        }

        private static void Application_ApplicationExit(object sender, EventArgs e)
        {
            AppDomain.CurrentDomain.ProcessExit -= Application_ApplicationExit;
            _cts.Cancel();
            _service?.Shutdown();
        }
    }
}
using System;
using System.Diagnostics;

namespace UiPath.Python.Host
{
    internal static class Program
    {
        private static PythonService _service = null;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main()
        {
            AppDomain.CurrentDomain.ProcessExit += Application_ApplicationExit;

            _service = new PythonService();
            _service.RunServer();
            Console.ReadLine();
        }

        private static void Application_ApplicationExit(object sender, EventArgs e)
        {
            AppDomain.CurrentDomain.ProcessExit -= Application_ApplicationExit;
            _service?.Shutdown();
        }
    }
}
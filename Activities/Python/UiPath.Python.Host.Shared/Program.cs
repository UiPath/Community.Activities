using System;
using System.Threading;

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

            //Console.ReadLine can throw under some unknown circumstances
            //Simulate waiting for key by sleeping forever
            //https://forum.uipath.com/t/python-scope-throws-an-error-on-the-latest-uipath-python-activities-1-9-0-and-net8/2752619/6
            Thread.Sleep(Timeout.Infinite);
        }

        private static void Application_ApplicationExit(object sender, EventArgs e)
        {
            AppDomain.CurrentDomain.ProcessExit -= Application_ApplicationExit;

            _service?.Shutdown();
        }
    }
}
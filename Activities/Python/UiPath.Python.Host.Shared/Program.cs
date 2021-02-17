using System;
using System.Windows.Forms;

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
            Application.ApplicationExit += Application_ApplicationExit;

            _service = new PythonService();
            _service.RunServer();
            Application.Run();
            Console.ReadLine();
        }

        private static void Application_ApplicationExit(object sender, EventArgs e)
        {
            Application.ApplicationExit -= Application_ApplicationExit;
            _service?.Shutdown();
        }
    }
}
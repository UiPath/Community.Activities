using System;
using System.Windows.Forms;

namespace UiPath.Python.Host
{
    static class Program
    {
        static PythonService _service = null;
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.ApplicationExit += Application_ApplicationExit;

            _service = new PythonService();
            _service.Start();
            Application.Run();
        }

        private static void Application_ApplicationExit(object sender, EventArgs e)
        {
            Application.ApplicationExit -= Application_ApplicationExit;
            _service?.Stop();
        }
    }
}

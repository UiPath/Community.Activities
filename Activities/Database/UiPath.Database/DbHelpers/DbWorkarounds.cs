using System;
using System.IO;
using System.Runtime.InteropServices;

namespace UiPath.Database
{
    public static class DbWorkarounds
    {
        private static string RelativePath = @"\..\runtimes\win-x64\native\Microsoft.Data.SqlClient.SNI.dll";

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr LoadLibrary(string libname);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern bool FreeLibrary(IntPtr hModule);

        public static void SNILoadWorkaround(bool isWindows = true)
        {
            //SNI workaround is necessary only for windows
            if(!isWindows)
                return;

            var asmLocation = GetAssemblyLocation();
            var path = Path.GetFullPath(asmLocation + RelativePath);

            IntPtr Handle = LoadLibrary(path);
            if (Handle == IntPtr.Zero)
            {
                int errorCode = Marshal.GetLastWin32Error();
                string errorMessage = string.Format("Failed to load library {0} (ErrorCode: {1})", path, errorCode);
                throw new Exception(errorMessage);
            }
        }

        private static string GetAssemblyLocation()
        {
            return typeof(DbWorkarounds).Assembly.Location;
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace UiPath.Data.ConnectionUI.Dialog.Workaround
{
    public static class DbWorkarounds
    {
#if NETCOREAPP
        private static string RelativePath = @"\..\runtimes\win-x64\native\Microsoft.Data.SqlClient.SNI.dll";
#endif
#if NETFRAMEWORK
        private static string RelativePath = Environment.Is64BitProcess ? @"\..\runtimes\win-x64\native\Microsoft.Data.SqlClient.SNI.dll" : @"\..\runtimes\win-x86\native\Microsoft.Data.SqlClient.SNI.x86.dll";
#endif

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr LoadLibrary(string libname);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern bool FreeLibrary(IntPtr hModule);

        public static void SNILoadWorkaround(bool isWindows = true)
        {
            //SNI workaround is necessary only for windows
            if(!isWindows)
                return;

            IntPtr Handle = LoadLibrary(Path.GetFullPath((typeof(DbWorkarounds).Assembly.CodeBase.Replace("file:///", "")) + RelativePath));

            if (Handle == IntPtr.Zero)
            {
                int errorCode = Marshal.GetLastWin32Error();
                throw new Exception(string.Format("Failed to load library (ErrorCode: {0})", errorCode));
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace UiPath.Database.Workaround
{
    public static class DbWorkarounds
    {
#if NETCOREAPP
        private const string RelativePath = @"\..\runtimes\win-x64\native\sni.dll";
#endif
#if NETFRAMEWORK
        private const string RelativePath = @"\..\runtimes\win-x86\native\sni.dll";
#endif

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr LoadLibrary(string libname);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern bool FreeLibrary(IntPtr hModule);

        public static void SNILoadWorkaround()
        {
            IntPtr Handle = LoadLibrary(Path.GetFullPath((typeof(DbWorkarounds).Assembly.CodeBase.Replace("file:///", "")) + RelativePath));

            if (Handle == IntPtr.Zero)
            {
                int errorCode = Marshal.GetLastWin32Error();
                throw new Exception(string.Format("Failed to load library (ErrorCode: {0})", errorCode));
            }
        }
    }
}

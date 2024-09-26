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

#if NETCOREAPP
        private static string GetAssemblyLocation()
        {
            return typeof(DbWorkarounds).Assembly.Location;
        }
#else
        private static string GetAssemblyLocation()
        {
            //on legacy still use the Assembly.CodeBase
            //on legacy Assembly.Location returns the shadow copy location and not the original location (so we won't find the sni dll relative to this path)
            var codeBase = typeof(DbWorkarounds).Assembly.CodeBase;
            bool isUNCPath = codeBase.StartsWith("file:////");
            var adjustdCodeBase = codeBase.Replace("file:///", "");
            //workaround: if UNC path, we should make sure that it stats with "//" (double) and not "/" (single)
            //since single "/" means relative path
            if (isUNCPath && !adjustdCodeBase.StartsWith("//"))
                adjustdCodeBase = "/" + adjustdCodeBase;

            return adjustdCodeBase;
        }
#endif

    }
}

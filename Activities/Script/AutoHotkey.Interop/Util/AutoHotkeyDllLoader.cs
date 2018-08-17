using System;
using System.IO;

namespace AutoHotkey.Interop.Util
{
    internal static class AutoHotkeyDllLoader
    {
        private static readonly Lazy<SafeLibraryHandle> dllHandle
            = new Lazy<SafeLibraryHandle>(LoadDll);
        

        internal static void EnsureDllIsLoaded() {
            if (dllHandle.IsValueCreated)
                return;

            var handle = dllHandle.Value;
        }

        private static SafeLibraryHandle LoadDll() {
            //determine if we should use x86/AutoHotkey.dll or x64/AutoHotkey.dll
            //then try to load it by the file directory or 
            //extract the embeded ones into a temp directory 
            //and load them

            string processor_type = ProcessorType.Is32Bit() ? "x86" : "x64";
            string relativePath = processor_type + "/AutoHotkey.dll";

            if (File.Exists(relativePath)) { 
                return SafeLibraryHandle.LoadLibrary(relativePath);
            }
            else { 
                return ExtractAndLoadEmbededResource(relativePath);
            }
        }

        private static SafeLibraryHandle ExtractAndLoadEmbededResource(string relativePath) {
            var assembly = typeof(AutoHotkeyEngine).Assembly;
            var resource = EmbededResourceHelper.FindByName(assembly, relativePath);

            if (resource != null) {
                string tempFolderPath = GetTempFolderPath();
                var output_file = Path.Combine(tempFolderPath, relativePath);
                EmbededResourceHelper.ExtractToFile(assembly, resource, output_file);
                return SafeLibraryHandle.LoadLibrary(output_file);
            }

            return null;
        }

        private static string GetTempFolderPath() {
            string temp = Path.GetTempPath();
            string ahk = "AutoHotkey.Interop";
            string version = typeof(AutoHotkeyEngine).Assembly.GetName().Version.ToString();
            return Path.Combine(temp, ahk, version);
        }
    }
}

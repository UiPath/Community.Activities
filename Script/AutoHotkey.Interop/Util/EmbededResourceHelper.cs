using System;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;

namespace AutoHotkey.Interop.Util
{
    internal static class EmbededResourceHelper
    {
        public static string FindByName(Assembly assembly, string path) {
            path = Regex.Replace(path, @"[/\\]", "."); //replace slashes with periods
            string path_with_dot_prefix = path.StartsWith(".") ? path : "." + path;
            
            var names = assembly.GetManifestResourceNames();

            foreach (var name in names) {
                if (name.EndsWith(path_with_dot_prefix, StringComparison.InvariantCultureIgnoreCase))
                    return name;
                if (name.Equals(path, StringComparison.InvariantCultureIgnoreCase))
                    return name;
            }

            return null;
        }
        

        public static void ExtractToFile(Assembly assembly, string embededResourceName, string outputFilePath) {
            string full_resource_name = FindByName(assembly, embededResourceName);

            if (full_resource_name == null)
                throw new FileNotFoundException(string.Format("Cannot find resource name of '{0}' in assembly '{1}'", embededResourceName, assembly.GetName().Name), embededResourceName);


            EnsureDirectoryExistsForFile(outputFilePath);

            using (var readStream = assembly.GetManifestResourceStream(full_resource_name))
            using (var writeStream = File.Open(outputFilePath, FileMode.Create)) {
                readStream.CopyTo(writeStream);
                readStream.Flush();
            }
        }

        private static void EnsureDirectoryExistsForFile(string targetFileName) {
            var absolutePath = Path.GetFullPath(targetFileName);
            var directoryPath = Path.GetDirectoryName(absolutePath);

            if (!Directory.Exists(directoryPath))
                Directory.CreateDirectory(directoryPath);
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace UiPath.Python.Impl
{
    /// <summary>
    /// The third-party assemblies the host process needs but is not packaged with. They are loaded here,
    /// in the calling process, from wherever it resolved them (the restored pythonnet package, the
    /// executor's System.Xaml), and their locations are handed to the host on its command line.
    /// </summary>
    internal static class HostAssemblies
    {
        internal static IReadOnlyDictionary<string, string> GetPaths()
        {
            var paths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            Add(paths, PythonRuntime());
            Add(paths, Xaml());
            return paths;
        }

        // Kept out of line so the type references are only resolved when these methods run.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Assembly PythonRuntime() => typeof(global::Python.Runtime.PythonEngine).Assembly;

        // UiPath.Python carries XmlnsDefinition, so the host must be able to resolve System.Xaml too.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Assembly Xaml() => typeof(System.Windows.Markup.XmlnsDefinitionAttribute).Assembly;

        private static void Add(Dictionary<string, string> paths, Assembly assembly)
        {
            var name = assembly.GetName().Name;
            var location = assembly.Location;
            if (string.IsNullOrEmpty(location) || !File.Exists(location))
                throw new InvalidOperationException($"Cannot pass assembly '{name}' to the Python host: it has no file on disk (location '{location}').");

            paths[name] = location;
        }
    }
}

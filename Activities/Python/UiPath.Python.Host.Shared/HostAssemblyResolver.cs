using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using UiPath.Shared.Service;

namespace UiPath.Python.Host
{
    /// <summary>
    /// Loads the third-party assemblies the client passed on the command line (see
    /// <see cref="HostAssemblyArguments"/>) instead of expecting them next to the host. Only assemblies
    /// the default context could not resolve reach the handler, so anything the runtime already
    /// provides (for example System.Xaml from Microsoft.WindowsDesktop.App) is left alone.
    /// </summary>
    internal static class HostAssemblyResolver
    {
        /// <summary>Registers the resolver, writing the reason to <paramref name="errorOutput"/> when the arguments are bad.</summary>
        internal static bool TryRegisterOrReport(IEnumerable<string> args, TextWriter errorOutput)
        {
            if (TryRegister(args, out var error))
                return true;

            errorOutput.WriteLine(error);
            return false;
        }

        internal static bool TryRegister(IEnumerable<string> args, out string error)
        {
            if (!HostAssemblyArguments.TryParse(args, out var assemblies, out error))
                return false;

            var missing = assemblies.FirstOrDefault(a => !File.Exists(a.Value));
            if (missing.Key != null)
            {
                error = $"Assembly '{missing.Key}' was not found at '{missing.Value}'.";
                return false;
            }

            if (assemblies.Count > 0)
                AssemblyLoadContext.Default.Resolving += (context, name) => Resolve(context, name, assemblies);

            return true;
        }

        internal static Assembly Resolve(AssemblyLoadContext context, AssemblyName name, IReadOnlyDictionary<string, string> assemblies)
        {
            if (name.Name == null || !assemblies.TryGetValue(name.Name, out var path))
                return null;

            var assembly = context.LoadFromAssemblyPath(path);
            if (!string.Equals(assembly.GetName().Name, name.Name, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"'{path}' is assembly '{assembly.GetName().Name}', expected '{name.Name}'.");

            return assembly;
        }
    }
}

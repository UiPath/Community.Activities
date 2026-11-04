using System;
using System.Collections.Generic;
using System.Linq;

namespace UiPath.Shared.Service
{
    /// <summary>
    /// Command-line contract between the client (which starts the host process) and the host: a map of
    /// assembly simple name to full path, one <c>Name=Path</c> argument per assembly. The names are
    /// constants owned by this repo, so splitting on the first separator is unambiguous; the path
    /// may itself contain the separator.
    /// </summary>
    internal static class HostAssemblyArguments
    {
        private const char Separator = '=';

        internal static IEnumerable<string> Format(IReadOnlyDictionary<string, string> assemblies)
        {
            return assemblies?.Select(a => $"{a.Key}{Separator}{a.Value}") ?? Enumerable.Empty<string>();
        }

        internal static bool TryParse(IEnumerable<string> args, out Dictionary<string, string> assemblies, out string error)
        {
            assemblies = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            error = null;

            foreach (var arg in args ?? Enumerable.Empty<string>())
            {
                var separatorIndex = arg.IndexOf(Separator);
                if (separatorIndex <= 0 || separatorIndex == arg.Length - 1)
                {
                    error = $"Invalid host argument '{arg}': expected <AssemblyName>{Separator}<FullPath>.";
                    return false;
                }

                var name = arg.Substring(0, separatorIndex);
                if (!assemblies.TryAdd(name, arg.Substring(separatorIndex + 1)))
                {
                    error = $"Duplicate host argument for assembly '{name}'.";
                    return false;
                }
            }

            return true;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Threading;

namespace UiPath.Database
{
    public static class DbWorkarounds
    {
        /// <summary>Describes a native library that ships inside a NuGet package.</summary>
        internal readonly record struct NativePackage(string Package, string MinVersion, string Module);

        internal static readonly NativePackage Sni    = new("microsoft.data.sqlclient.sni.runtime", "5.2.0", "Microsoft.Data.SqlClient.SNI");
        internal static readonly NativePackage Sqlite = new("sqlitepclraw.lib.e_sqlite3",           "2.1.6", "e_sqlite3");

        private static int _resolverRegistered;
        private static readonly ConditionalWeakTable<AssemblyLoadContext, object> _registeredContexts = new();
        private static readonly object _contextLock = new();
        private static readonly object _sentinel = new();

        /// <summary>
        /// Registers a <see cref="AssemblyLoadContext.ResolvingUnmanagedDll"/> handler on every
        /// existing <see cref="AssemblyLoadContext"/> and on any context created in the future
        /// (detected via <see cref="AppDomain.AssemblyLoad"/>), so that native libraries
        /// (SNI, e_sqlite3) are resolved from the local NuGet cache on demand regardless of
        /// which load context hosts the calling assembly.
        /// Safe to call multiple times — registration happens only once.
        /// </summary>
        public static void RegisterNativeLibraryResolver()
        {
            if (Interlocked.CompareExchange(ref _resolverRegistered, 1, 0) != 0)
                return;

            // Subscribe to future assemblies first, then enumerate existing contexts.
            // This ordering narrows (but cannot fully close) the race window: an ALC created
            // after the subscription but before the enumeration will be registered twice,
            // which RegisterOnContext handles safely. An ALC created before the subscription
            // and after the enumeration would be missed, but that window is vanishingly small
            // and the consequence is merely that the resolver is not active in that one context.
            AppDomain.CurrentDomain.AssemblyLoad += OnAssemblyLoaded;

            foreach (var ctx in AssemblyLoadContext.All)
                RegisterOnContext(ctx);
        }

        private static void OnAssemblyLoaded(object sender, AssemblyLoadEventArgs args)
        {
            var ctx = AssemblyLoadContext.GetLoadContext(args.LoadedAssembly);
            if (ctx != null)
                RegisterOnContext(ctx);
        }

        private static void RegisterOnContext(AssemblyLoadContext ctx)
        {
            lock (_contextLock)
            {
                if (!_registeredContexts.TryGetValue(ctx, out _))
                {
                    _registeredContexts.Add(ctx, _sentinel);
                    ctx.ResolvingUnmanagedDll += ResolveUnmanagedDll;
                }
            }
        }

        private static IntPtr ResolveUnmanagedDll(Assembly assembly, string libraryName)
        {
            try
            {
                if (libraryName.StartsWith(Sqlite.Module, StringComparison.OrdinalIgnoreCase))
                {
                    var path = ResolveNativeLibraryPath(Sqlite);
                    return path != null ? NativeLibrary.Load(path) : IntPtr.Zero;
                }

                if (libraryName.StartsWith(Sni.Module, StringComparison.OrdinalIgnoreCase))
                {
                    var path = ResolveNativeLibraryPath(Sni);
                    return path != null ? NativeLibrary.Load(path) : IntPtr.Zero;
                }
            }
            catch (Exception ex)
            {
                // Return zero so the runtime can continue with its own fallback chain.
                Trace.TraceWarning($"[DbWorkarounds] Failed to resolve native library '{libraryName}': {ex}");
            }

            return IntPtr.Zero;
        }

        /// <summary>Resolves the full path to a native library file from the NuGet cache; returns <c>null</c> if not found.</summary>
        private static string ResolveNativeLibraryPath(NativePackage library)
        {
            var packageVersionPath = FindPackageVersionPath(library.Package, library.MinVersion);
            var rid = GetCurrentRid();
            var moduleFileName = GetNativeLibraryFileName(library.Module);
            return FindNativeLibraryInPackage(packageVersionPath, rid, moduleFileName);
        }

        private static string GetAssemblyLocation()
        {
            return typeof(DbWorkarounds).Assembly.Location;
        }

        private static IEnumerable<string> NugetPackageRoots()
        {
            // 1. Assembly-relative: works when this dll is itself installed as a NuGet
            //    package at {packages}\{pkg}\{ver}\lib\{tfm}\{dll} — 4 levels up.
            var assemblyDir = Path.GetDirectoryName(GetAssemblyLocation());
            if (!string.IsNullOrEmpty(assemblyDir))
                yield return Path.GetFullPath(Path.Combine(assemblyDir, "..", "..", "..", ".."));

            // 2. Explicit override (CI pipelines, custom restore paths)
            var fromEnv = Environment.GetEnvironmentVariable("NUGET_PACKAGES");
            if (!string.IsNullOrWhiteSpace(fromEnv))
                yield return fromEnv;

            // 3. Default global NuGet cache
            yield return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".nuget", "packages");
        }

        private static string FindPackageVersionPath(string package, string minVersion)
        {
            // The same package folder may exist in more than one cache root with
            // different versions installed in each, so scan all roots and pick
            // the lowest stable version that satisfies the minimum requirement.
            // If no stable version is found, fall back to the lowest pre-release
            // version that satisfies the minimum requirement.
            var minVer = Version.Parse(minVersion);
            var roots = NugetPackageRoots().ToList();

            var candidates = roots
                .Select(root => Path.Combine(root, package.ToLowerInvariant()))
                .Where(Directory.Exists)
                .SelectMany(d => TryGetDirectories(d))
                .Select(d => (path: d, ver: TryParseVersion(Path.GetFileName(d), out bool isPrerelease), isPrerelease))
                .Where(x => x.ver != null && x.ver >= minVer)
                .ToList();

            var best = candidates.Where(x => !x.isPrerelease).OrderBy(x => x.ver).FirstOrDefault();

            if (best.path is null)
                best = candidates.Where(x => x.isPrerelease).OrderBy(x => x.ver).FirstOrDefault();

            if (best.path is null)
                throw new DirectoryNotFoundException(
                    $"No installed version >= {minVersion} found for NuGet package '{package}' " +
                    $"in: {string.Join(", ", roots)}");

            return best.path;
        }

        private static IEnumerable<string> TryGetDirectories(string path)
        {
            try { return Directory.GetDirectories(path); }
            catch (Exception) { return Enumerable.Empty<string>(); }
        }

        private static IEnumerable<string> TryGetFiles(string path, string pattern)
        {
            try { return Directory.GetFiles(path, pattern, SearchOption.AllDirectories); }
            catch (Exception) { return Enumerable.Empty<string>(); }
        }

        private static Version TryParseVersion(string versionStr, out bool isPrerelease)
        {
            var dashIndex = versionStr.IndexOf('-');
            isPrerelease = dashIndex >= 0;

            // Strip pre-release suffix before parsing (e.g. "2.1.6-preview1" -> "2.1.6")
            var numericPart = isPrerelease ? versionStr[..dashIndex] : versionStr;

            return Version.TryParse(numericPart, out var ver) ? ver : null;
        }

        private static string GetNativeLibraryFileName(string module)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return $"{module}.dll";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return $"lib{module}.dylib";
            return $"lib{module}.so";
        }

        private static string GetCurrentRid()
        {
            string os;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                os = "win";
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                os = "linux";
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                os = "osx";
            else
                throw new PlatformNotSupportedException("Unsupported operating system.");

            string arch = RuntimeInformation.ProcessArchitecture switch
            {
                Architecture.X64 => "x64",
                Architecture.X86 => "x86",
                Architecture.Arm64 => "arm64",
                Architecture.Arm => "arm",
                _ => throw new PlatformNotSupportedException(
                    $"Unsupported architecture: {RuntimeInformation.ProcessArchitecture}")
            };

            return $"{os}-{arch}";
        }

        private static string FindNativeLibraryInPackage(string packageVersionPath, string rid, string module)
        {
            // 1. Standard layout: runtimes/{rid}/native/{module}
            var standard = Path.Combine(packageVersionPath, "runtimes", rid, "native", module);
            if (File.Exists(standard))
                return standard;

            // 2. Any subdirectory under runtimes/{rid}/ (e.g. nativeassets/uap10.0/)
            var ridDir = Path.Combine(packageVersionPath, "runtimes", rid);
            if (Directory.Exists(ridDir))
            {
                var found = TryGetFiles(ridDir, module);
                if (found.Any())
                    return found.First();
            }

            // 3. Fallback: search the whole package version folder
            var fallback = TryGetFiles(packageVersionPath, module);
            if (fallback.Any())
                return fallback.First();

            throw new FileNotFoundException(
                $"Native library '{module}' not found in '{packageVersionPath}' for RID '{rid}'.");
        }
    }
}

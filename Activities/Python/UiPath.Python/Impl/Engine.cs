using Python.Runtime;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace UiPath.Python.Impl
{
    internal class Engine : IEngine
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetDllDirectory(string lpPathName);

        private readonly bool _isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        private bool _initialized;

        #region Runtime info

        private readonly Version _version;
        private readonly string _path;
        private readonly string _libraryPath;

        #endregion Runtime info

        internal Engine(Version version, string path, string libraryPath)
        {
            _version = version;
            _path = path;
            _libraryPath = libraryPath;
        }

        #region IEngine

        public Version Version => _version;

        public async Task Initialize(string workingFolder, CancellationToken ct, double timeout)
        {
            if (!_initialized)
            {
                lock (this)
                {
                    if (!_initialized)
                    {
                        try
                        {
                            ct.ThrowIfCancellationRequested();
                            Trace.TraceInformation($"Initializing Python runtime using version {_version} and path {_path}");
                            Stopwatch sw = Stopwatch.StartNew();

                            // A real venv (activated normally, e.g. <venv>\Scripts\python.exe)
                            // always disables the PEP-370 user-site directory on its own. Our
                            // embedded interpreter never goes through that activation path, so
                            // nothing does this for us — without it, a native package installed in
                            // both the venv and the user-site directory (e.g. pywin32) can resolve
                            // its Python module from one and its native DLL dependency from the
                            // other, mismatched, copy (STUD-81085). See ConfigureRuntime for how
                            // this is used.
                            var venv = _version == Version.Python_310 ? VenvDetection.GetVenvInfo(_path) : null;

                            ConfigureRuntime(venv);

                            PythonEngine.Initialize();

                            ct.ThrowIfCancellationRequested();

                            PostInitializationVenvSetup(venv);
                            PythonEngine.BeginAllowThreads();

                            sw.Stop();
                            Trace.TraceInformation($"Engine intialization took {sw.ElapsedMilliseconds} ms");
                            _initialized = true;
                        }
                        catch (Exception e)
                        {
                            Trace.TraceError($"Python runtime initialization exception: {e}");
                            throw;
                        }
                    }
                    else
                    {
                        Trace.TraceInformation($"Using cached Python runtime version {_version} and path {_path}");
                    }
                }

                if (!workingFolder.IsNullOrEmpty())
                {
                    var code = GetInitializationScript();
                    var module = await LoadScript(code, ct);
                    await InvokeMethod(module, "setWorkingFolder", new[] { workingFolder }, ct);
                }
            }
        }

        /// <summary>
        /// Sets up everything <see cref="PythonEngine.Initialize"/> needs before it's called: the
        /// native DLL search directory, which pythonnet-loaded library to use, <see
        /// cref="PythonEngine.PythonHome"/>, and whether to suppress <c>site.main()</c>'s automatic
        /// run for <paramref name="venv"/>.
        ///
        /// <para>
        /// A venv's own folder is not a valid PythonHome: it only has Lib\site-packages, not the
        /// standard library (Lib\encodings etc.), so pointing the native interpreter at it fails at
        /// the very first import with "Fatal Python error: Failed to import encodings module". Use
        /// the base install recorded in the venv's own pyvenv.cfg instead — exactly what a
        /// normally-activated venv resolves to on its own — falling back to a real prefix derived
        /// from LibraryPath itself (validated, mandatory — see ResolvePrefixFromLibraryPath)
        /// whenever that recorded base install doesn't actually carry a stdlib either (e.g. a
        /// Microsoft Store Python's app-execution-alias folder, or a pyvenv.cfg missing "home"
        /// entirely).
        /// </para>
        ///
        /// <para>
        /// For a default venv (no --system-site-packages), suppresses both the PEP-370 user-site
        /// leak (STUD-81085) and a second, distinct leak path found by inspecting a real venv's
        /// actual sys.path: CPython's own site.main() also unconditionally adds the *base
        /// install's* own site-packages (site.addsitepackages() against sys.prefix/exec_prefix,
        /// still pointing at the base install at this point). PYTHONNOUSERSITE would only ever have
        /// covered the first of these — SetNoSiteFlag (Py_NoSiteFlag) disables site.main()'s
        /// automatic run entirely, so *neither* ever gets added in the first place: "prevent, don't
        /// clean up after" — a cleanup-after-the-fact fix couldn't undo any .pth-triggered side
        /// effects, e.g. os.add_dll_directory calls, that already ran by the time managed code
        /// regains control. It's also an in-memory flag on this process's loaded Python DLL, never
        /// written to os.environ — unlike an env-var-based approach, it can't be defeated by (or
        /// leak into) whatever PYTHONNOUSERSITE the ambient environment happens to already carry.
        /// `site` itself is still importable — this only skips its automatic invocation at startup
        /// — so the explicit site.addsitedir() call in PostInitializationVenvSetup for the venv's
        /// own site-packages, and the sitecustomize/usercustomize handling there, both keep
        /// working.
        /// </para>
        ///
        /// <para>
        /// Py_NoSiteFlag is deprecated since CPython 3.12, with removal planned for 3.15
        /// (VersionExtensions._supportedRuntimeVersions currently tops out at 3.14): before adding
        /// 3.15+ support, confirm this flag still applies — its removal would silently re-open the
        /// original STUD-81085 leak rather than surface an error. The forward-looking replacement
        /// is PyConfig.site_import via Py_InitializeFromConfig.
        /// </para>
        ///
        /// <para>
        /// SetNoSiteFlag must be called after Runtime.PythonDLL/PythonEngine.PythonHome are set,
        /// not before — calling it earlier left Runtime.PythonDLL null by the time the host tried
        /// to use it (and, per a known pythonnet issue, SetNoSiteFlag itself can be silently
        /// ignored on Windows unless another PythonEngine call already preceded it — PythonHome,
        /// set just above, already satisfies that).
        /// </para>
        /// </summary>
        private void ConfigureRuntime(VenvDetection.VenvInfo venv)
        {
            if (_isWindows && !_path.IsNullOrEmpty())
                SetDllDirectory(Path.GetFullPath(_path));

            if (!_libraryPath.IsNullOrEmpty())
                Runtime.PythonDLL = _libraryPath;

            var pythonHome = venv != null ? ResolvePythonHome(venv.Home) : _path;
            if (!HasStdlib(pythonHome))
                pythonHome = ResolvePrefixFromLibraryPath(_libraryPath);

            if (!pythonHome.IsNullOrEmpty())
                PythonEngine.PythonHome = pythonHome;

            if (venv != null && venv.ShouldDisableUserSite)
                PythonEngine.SetNoSiteFlag();
        }

        public Task Release()
        {
            lock (this)
            {
                if (_initialized)
                {
                    PythonEngine.Shutdown();
                    _initialized = false;
                }

                return Task.FromResult(true);
            }
        }

        public Task<PythonObject> LoadScript(string code, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            return RunSTA(() =>
            {
                using (Py.GIL())
                {
                    Trace.TraceInformation("Trying to load Python script");
                    Stopwatch sw = Stopwatch.StartNew();
                    try
                    {
                        var module = PyModule.FromString(GetModuleName(code), code);
                        return new PythonObject(module);
                    }
                    catch (Exception e)
                    {
                        Trace.TraceError($"Python LoadScript exception: {e}");
                        ExceptionDispatchInfo.Capture(e).Throw();
                        return null;
                    }
                    finally
                    {
                        sw.Stop();
                        Trace.TraceInformation($"Load script took {sw.ElapsedMilliseconds} ms");
                    }
                }
            });
        }

        public Task<PythonObject> InvokeMethod(PythonObject instance, string method, IEnumerable<object> args, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.Run(() =>
            {
                using (Py.GIL())
                {
                    args ??= Enumerable.Empty<object>();
                    var paramsPy = args.Select(ConverterExtension.ToPython).ToArray();
                    Trace.TraceInformation("Trying to execute Python method");
                    Stopwatch sw = Stopwatch.StartNew();
                    try
                    {
                        var pyInstance = (PyObject)instance.PyObject;
                        var result = pyInstance.InvokeMethod(method, paramsPy);
                        return new PythonObject(result);
                    }
                    catch (Exception e)
                    {
                        Trace.TraceError($"Python InvokeMethod exception: {e}");
                        ExceptionDispatchInfo.Capture(e).Throw();
                        return null;
                    }
                    finally
                    {
                        sw.Stop();
                        Trace.TraceInformation($"Method execution took {sw.ElapsedMilliseconds} ms");
                    }
                }
            }, cancellationToken);
        }

        public Task Execute(string code, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return RunSTA(() =>
            {
                using (Py.GIL())
                {
                    Trace.TraceInformation("Trying to execute Python script");
                    Stopwatch sw = Stopwatch.StartNew();
                    try
                    {
                        PythonEngine.Exec(code);
                    }
                    catch (Exception e)
                    {
                        Trace.TraceError($"Python Execute exception: {e}");
                        ExceptionDispatchInfo.Capture(e).Throw();
                    }
                    finally
                    {
                        sw.Stop();
                        Trace.TraceInformation($"Script execution took {sw.ElapsedMilliseconds} ms");
                    }
                }

                return true;
            });
        }

        public object Convert(PythonObject obj, Type t)
        {
            using (Py.GIL())
            {
                Trace.TraceInformation($"Trying to convert Python object to type {t}");
                return obj.AsManagedType(t);
            }
        }

        #endregion IEngine

        public void Dispose()
        {
            // see Release method, for the moment the runtime is cached
        }

        #region script name caching

        private readonly Dictionary<string, string> _cachedModules = new Dictionary<string, string>();

        /// <summary>
        /// gets the module name based on the script content hash
        /// </summary>
        /// <param name="script"></param>
        /// <returns></returns>
        private string GetModuleName(string script)
        {
            string hash = Hash(script);
            if (_cachedModules.TryGetValue(hash, out string moduleName))
            {
                return moduleName;
            }

            lock (this)
            {
                if (_cachedModules.TryGetValue(hash, out moduleName))
                {
                    return moduleName;
                }

                moduleName = Guid.NewGuid().ToString();
                _cachedModules.Add(hash, moduleName);
            }

            return moduleName;
        }

        private static string Hash(string input)
        {
            using (MD5 md5 = MD5.Create())
            {
                byte[] bytes = md5.ComputeHash(Encoding.Unicode.GetBytes(input));
                return BitConverter.ToString(bytes);
            }
        }

        private string GetInitializationScript()
        {
            const string resourceName = "UiPath.Python.Scripts.Init.py";
            var asm = typeof(Engine).Assembly;
            using var resourceStream = asm.GetManifestResourceStream(resourceName)
                ?? throw new InvalidOperationException($"Embedded resource '{resourceName}' was not found in assembly '{asm.FullName}'.");
            using var reader = new StreamReader(resourceStream);
            return reader.ReadToEnd();
        }

        /// <summary>
        /// Normalizes a venv's pyvenv.cfg "home" value into a PythonHome-compatible prefix. On
        /// Windows "home" already is the install root (no adjustment needed). On POSIX it records
        /// the base install's bin folder (e.g. "/usr/bin"), one level below the prefix PythonHome
        /// actually expects (e.g. "/usr") — strip it when present.
        /// </summary>
        private static string ResolvePythonHome(string venvHome)
        {
            if (venvHome.IsNullOrEmpty())
                return null;

            var trimmed = venvHome.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var lastSegment = Path.GetFileName(trimmed);
            if (string.Equals(lastSegment, "bin", StringComparison.Ordinal))
                return Path.GetDirectoryName(trimmed);

            return venvHome;
        }

        /// <summary>
        /// Whether <paramref name="pythonHome"/> actually carries a standard library, i.e. is a
        /// real, complete Python install root rather than e.g. a Microsoft Store app-execution-
        /// alias folder (reparse-point exe stubs only) or a venv root itself. Also recognizes a
        /// <c>*._pth</c> file directly in this folder as valid evidence of a home: CPython's
        /// <c>._pth</c>-based isolation (used by the official Windows embeddable distribution, but
        /// not Windows-exclusive) resolves its stdlib from a bundled zip next to the interpreter
        /// rather than an unpacked Lib folder, so the folder-based checks below would otherwise
        /// wrongly treat a perfectly valid embeddable-style home as having no stdlib at all.
        /// </summary>
        private static bool HasStdlib(string pythonHome)
        {
            if (pythonHome.IsNullOrEmpty() || !Directory.Exists(pythonHome))
                return false;

            if (Directory.EnumerateFiles(pythonHome, "*._pth").Any())
                return true;

            return RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? Directory.Exists(WindowsStdlibLandmark(pythonHome))
                : HasPosixStdlib(pythonHome);
        }

        // "Lib" (capital L) and "lib" (lowercase) below are each the real, fixed folder name
        // CPython's own installer/build produces on that platform — not a style choice. Exposed
        // (internal) so a test can assert on the exact, case-sensitive string each platform
        // checks for: this repo's CI only runs Windows agents, where NTFS's default
        // case-insensitive resolution means a real Directory.Exists call can't distinguish "Lib"
        // from "lib" on disk — so the only way to actually pin the casing down here is to assert
        // the literal path string being built, rather than relying on filesystem lookup behavior.
        internal static string WindowsStdlibLandmark(string pythonHome) => Path.Combine(pythonHome, "Lib", "encodings");

        internal static string PosixStdlibDirectory(string pythonHome) => Path.Combine(pythonHome, "lib");

        private static bool HasPosixStdlib(string pythonHome)
        {
            var libPath = PosixStdlibDirectory(pythonHome);
            return Directory.Exists(libPath)
                && Directory.GetDirectories(libPath, "python*").Any(dir => Directory.Exists(Path.Combine(dir, "encodings")));
        }

        /// <summary>
        /// Derives a real Python prefix from <paramref name="libraryPath"/> itself, for when the
        /// venv's/Path's declared home doesn't carry a stdlib. On Windows the DLL sits directly at
        /// the prefix root, so its own directory normally already qualifies — confirmed by the walk
        /// below succeeding on the first iteration there. On POSIX the shared library sits one or
        /// more levels *below* the prefix — plain <c>lib/libpythonX.Y.so</c>, or a multiarch triplet
        /// like <c>lib/x86_64-linux-gnu/libpythonX.Y.so</c> — so blindly taking the library's own
        /// directory (as an earlier version of this fix did) yields something like
        /// <c>/opt/python/lib</c> rather than the real <c>/opt/python</c>, which itself has no
        /// stdlib directly under it either. Walking up ancestors, testing <see cref="HasStdlib"/> at
        /// each level, finds the real prefix on both platforms without hardcoding how many levels
        /// separate the library from it.
        /// </summary>
        internal static string ResolvePrefixFromLibraryPath(string libraryPath)
        {
            if (libraryPath.IsNullOrEmpty())
                return null;

            var dir = Path.GetDirectoryName(libraryPath);
            while (!dir.IsNullOrEmpty())
            {
                if (HasStdlib(dir))
                    return dir;

                var parent = Path.GetDirectoryName(dir);
                if (string.Equals(parent, dir, StringComparison.Ordinal))
                    break;
                dir = parent;
            }

            return null;
        }

        private static string GetEnvSitePackagesPath(string venvPath)
        {
            string sitePackages;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                sitePackages = Path.Combine(venvPath, "Lib", "site-packages");
            }
            else
            {
                var libPath = Path.Combine(venvPath, "lib");
                var pythonDir = Directory.GetDirectories(libPath, "python*").FirstOrDefault()
                    ?? throw new DirectoryNotFoundException($"No python* directory found under {libPath}");
                sitePackages = Path.Combine(pythonDir, "site-packages");
            }

            return sitePackages;
        }

        private static void PostInitializationVenvSetup(VenvDetection.VenvInfo venv)
        {
            if (venv != null)
            {
                using (Py.GIL())
                {
                    dynamic sys = Py.Import("sys");
                    dynamic site = Py.Import("site");

                    sys.prefix = venv.Root;
                    sys.exec_prefix = venv.Root;

                    var sitePackagesPath = GetEnvSitePackagesPath(venv.Root);
                    site.addsitedir(sitePackagesPath);

                    if ((bool)sys.path.__contains__(sitePackagesPath))
                        sys.path.remove(sitePackagesPath);

                    sys.path.insert(0, sitePackagesPath);

                    if (venv.ShouldDisableUserSite)
                    {
                        // SetNoSiteFlag (see ConfigureRuntime) skipped site.main() entirely for
                        // this default venv — which means it also skipped the plain, non-path
                        // parts of site.main() itself: setquit()/setcopyright()/sethelper(), the
                        // module-level functions that install the quit/exit/help/copyright/
                        // credits/license builtins. Without them a script calling exit() (a common
                        // RPA pattern, however discouraged) dies with a NameError that gives no
                        // hint it's related to venv handling. enablerlcompleter() is deliberately
                        // not restored — it only registers an interactive-startup readline hook,
                        // irrelevant to a non-interactive embedded script.
                        site.setquit();
                        site.setcopyright();
                        site.sethelper();

                        // Nothing was ever imported/cached under SetNoSiteFlag — the stdlib paths
                        // (including the base install's own Lib) are still on sys.path under
                        // Py_NoSiteFlag, so this unconditionally picks up either the venv's own
                        // sitecustomize.py or a base-install Lib\sitecustomize.py (corporate
                        // proxy/logging setup etc.), exactly like it did before this whole fix and
                        // like a natively-activated default venv does.
                        site.execsitecustomize();
                    }
                    else if (File.Exists(Path.Combine(sitePackagesPath, "sitecustomize.py")))
                    {
                        // --system-site-packages: SetNoSiteFlag was *not* set, so site.main()
                        // already ran during PythonEngine.Initialize() above and may already have
                        // cached sitecustomize from whatever the base/user site resolved to
                        // sys.path first — before the venv's own site-packages, just inserted
                        // above, ever got a chance to take precedence. Only pop and re-import when
                        // the venv actually has its own copy, so it wins as intended; when it
                        // doesn't, the module already cached (already the correct,
                        // highest-priority one in that case) is left untouched, avoiding running
                        // its side effects a second time.
                        sys.modules.pop("sitecustomize", null);
                        site.execsitecustomize();
                    }

                    // usercustomize.py lives in the *user-site* directory, never in a venv's own
                    // site-packages, and by the time we get here site.main() (when it ran, i.e.
                    // the --system-site-packages case) already handled it via its own "if
                    // ENABLE_USER_SITE:" guard — there is nothing left for this venv-specific setup
                    // to do for it, in either case. Deliberately not calling execusercustomize()
                    // ourselves, consistent with not touching user-site handling at all here.
                }
            }
        }

        #endregion script name caching

        #region STA

        private Task<T> RunSTA<T>(Func<T> func)
        {
            var tcs = new TaskCompletionSource<T>();
            Thread thread = new Thread(() =>
            {
                try
                {
                    tcs.SetResult(func());
                }
                catch (Exception e)
                {
                    tcs.SetException(e);
                }
            });
            if (_isWindows)
                thread.SetApartmentState(ApartmentState.STA);
            thread.Start();

            return tcs.Task;
        }

        #endregion STA
    }
}

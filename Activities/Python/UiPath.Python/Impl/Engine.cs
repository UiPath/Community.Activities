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

                            // Detected before Initialize(): a real venv (activated normally, e.g.
                            // <venv>\Scripts\python.exe) always disables the PEP-370 user-site
                            // directory on its own. Our embedded interpreter never goes through that
                            // activation path, so nothing does this for us — without it, a native
                            // package installed in both the venv and the user-site directory (e.g.
                            // pywin32) can resolve its Python module from one and its native DLL
                            // dependency from the other, mismatched, copy (STUD-81085). Suppression
                            // itself happens below, via PythonEngine.SetNoSiteFlag() — see that call
                            // for why (it also closes a second, related leak path, and is immune to
                            // whatever PYTHONNOUSERSITE happens to already be set in the ambient
                            // environment, which an env-var-based approach was not).
                            var venv = _version == Version.Python_310 ? VenvDetection.GetVenvInfo(_path) : null;

                            if (_isWindows && !_path.IsNullOrEmpty())
                                SetDllDirectory(Path.GetFullPath(_path));

                            if (!_libraryPath.IsNullOrEmpty())
                                Runtime.PythonDLL = _libraryPath;

                            // A venv's own folder is not a valid PythonHome: it only has
                            // Lib\site-packages, not the standard library (Lib\encodings etc.), so
                            // pointing the native interpreter at it fails at the very first import
                            // with "Fatal Python error: Failed to import encodings module". Use the
                            // base install recorded in the venv's own pyvenv.cfg instead — exactly
                            // what a normally-activated venv resolves to on its own. EngineProvider
                            // already validated that base install's version matches _libraryPath's.
                            var pythonHome = venv != null ? ResolvePythonHome(venv.Home) : _path;
                            if (!pythonHome.IsNullOrEmpty())
                                PythonEngine.PythonHome = pythonHome;

                            // For a default venv (no --system-site-packages), suppresses both the
                            // PEP-370 user-site leak (STUD-81085) and a second, distinct leak path
                            // found by inspecting a real venv's actual sys.path: CPython's own
                            // site.main() also unconditionally adds the *base install's* own
                            // site-packages (site.addsitepackages() against sys.prefix/exec_prefix,
                            // still pointing at the base install at this point). PYTHONNOUSERSITE
                            // would only ever have covered the first of these — SetNoSiteFlag
                            // (Py_NoSiteFlag) disables site.main()'s automatic run entirely, so
                            // *neither* ever gets added in the first place: "prevent, don't clean up
                            // after" — a cleanup-after-the-fact fix couldn't undo any .pth-triggered
                            // side effects, e.g. os.add_dll_directory calls, that already ran by the
                            // time managed code regains control. It's also an in-memory flag on this
                            // process's loaded Python DLL, never written to os.environ — unlike an
                            // env-var-based approach, it can't be defeated by (or leak into) whatever
                            // PYTHONNOUSERSITE the ambient environment happens to already carry, which
                            // is exactly the failure mode found in Controller.cs's
                            // ClearUserSiteEnvironmentOverride for the --system-site-packages case.
                            // `site` itself is still importable —
                            // this only skips its automatic invocation at startup — so the explicit
                            // site.addsitedir() call in PostInitializationVenvSetup for the venv's own
                            // site-packages, and the site.execsitecustomize() call there preserving
                            // sitecustomize.py support, both keep working. Must come after
                            // Runtime.PythonDLL/PythonHome are set, not before — calling it earlier
                            // left Runtime.PythonDLL null by the time the host tried to use it (and,
                            // per a known pythonnet issue, SetNoSiteFlag itself can be silently
                            // ignored on Windows unless another PythonEngine call already preceded
                            // it — PythonHome, set just above, already satisfies that).
                            if (venv != null && venv.ShouldDisableUserSite)
                                PythonEngine.SetNoSiteFlag();

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

        private void PostInitializationVenvSetup(VenvDetection.VenvInfo venv)
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

                    // SetNoSiteFlag (see Initialize()) skips site.main() entirely for a default
                    // venv, which also skips its sitecustomize.py auto-import — some environments
                    // rely on that for corporate setup (proxies, logging, etc.), and it did run
                    // today before this change, so preserve it explicitly. Safe to call even when
                    // SetNoSiteFlag wasn't set (--system-site-packages venvs): site.main() already
                    // ran it there, and re-importing an already-imported module is a no-op.
                    // Deliberately not calling execusercustomize() — its user-site counterpart,
                    // consistent with suppressing user-site itself.
                    site.execsitecustomize();
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

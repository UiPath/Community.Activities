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

                            if (_isWindows && !_path.IsNullOrEmpty())
                                SetDllDirectory(Path.GetFullPath(_path));

                            if (!_libraryPath.IsNullOrEmpty())
                                Runtime.PythonDLL = _libraryPath;

                            if (!_path.IsNullOrEmpty())
                                PythonEngine.PythonHome = _path;

                            PythonEngine.Initialize();

                            ct.ThrowIfCancellationRequested();

                            PostInitializationVenvSetup();
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
            var asm = typeof(Engine).Assembly;
            using var str = asm.GetManifestResourceStream("UiPath.Python.Scripts.Init.py");
            var reader = new StreamReader(str);
            return reader.ReadToEnd();
        }

        private static bool IsVenv(string path) => File.Exists(Path.Combine(path, "pyvenv.cfg"));

        private static string GetVenvPath(string venvPath, int maxLevels = 3)
        {
            if (string.IsNullOrEmpty(venvPath) || maxLevels == 0)
                return null;
            else if (IsVenv(venvPath))
                return venvPath;
            else
                return GetVenvPath(Path.GetDirectoryName(venvPath), maxLevels - 1);
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

        private void PostInitializationVenvSetup()
        {
            if (_version != Version.Python_310)
                return;

            var venvPath = GetVenvPath(_path);
            if (!string.IsNullOrWhiteSpace(venvPath))
            {
                using (Py.GIL())
                {
                    dynamic sys = Py.Import("sys");
                    dynamic site = Py.Import("site");

                    sys.prefix = venvPath;
                    sys.exec_prefix = venvPath;

                    var sitePackagesPath = GetEnvSitePackagesPath(venvPath);
                    site.addsitedir(sitePackagesPath);

                    if ((bool)sys.path.__contains__(sitePackagesPath))
                        sys.path.remove(sitePackagesPath);

                    sys.path.insert(0, sitePackagesPath);
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

using Nito.KitchenSink.Dynamic;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
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

        #region Python Runtime

        /// <summary>
        /// see:
        /// https://github.com/pythonnet/pythonnet/blob/master/src/runtime/pythonengine.cs
        /// https://github.com/pythonnet/pythonnet/blob/master/src/runtime/pyobject.cs
        /// </summary>
        private const string PythonEngineTypeName = "Python.Runtime.PythonEngine";
        private const string PythonRuntimeTypeName = "Python.Runtime.Runtime";

        private const string PythonObjectTypeName = "Python.Runtime.PyObject";
        private const string PythonModuleTypeName = "Python.Runtime.PyModule";
        private const string PyTypeName = "Python.Runtime.Py";
        private const string ConverterExtensionTypeName = "Python.Runtime.ConverterExtension";

        private dynamic _pyEngine = null;
        private dynamic _pyRuntime = null;
        private dynamic _pyObject = null;
        private dynamic _pyModule = null;
        private dynamic _py = null;
        private dynamic _pyConverterExtension = null;
        private object _pythreads;

        // TODO: find a nicer way for method invocation
        private const string PythonObjectInvokeMethodName = "InvokeMethod";

        private const string ToPythonMethodName = "ToPython";

        private Type _pyObjType = null;
        private MethodInfo _toPythonMethod = null;
        private MethodInfo _pyObjInvokeMethod = null;
        private bool _isWindows = true;
        #endregion Python Runtime

        #region Caching

        private bool _initialized = false;

        #endregion Caching

        #region Runtime info

        private Version _version;
        private string _path;
        private string _libraryPath;

        #endregion Runtime info

        internal Engine(Version version, string path, string libraryPath)
        {
            _version = version;
            _path = path;
            _libraryPath = libraryPath;
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                _isWindows = false;
        }

        #region IEngine

        public Version Version { get { return _version; } }

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

                            // needed to find the Python dll on Windows
                            if (_isWindows)
                                SetDllDirectory(Path.GetFullPath(_path));

                            // load the dedicated Python.Runtime.XX.dll
                            string path = Path.GetDirectoryName(new Uri(Assembly.GetAssembly(GetType()).Location).LocalPath);
                            path = Path.Combine(path, (IntPtr.Size == 8) ? "x64" : "x86");
                            path = Path.Combine(path, _version.GetAssemblyName());

                            Assembly assembly = Assembly.LoadFile(path);
                            ct.ThrowIfCancellationRequested();

                            InitializeRuntime(assembly);
                            ct.ThrowIfCancellationRequested();

                            if (_version == Version.Python_310)
                            {
                                if (!string.IsNullOrEmpty(_libraryPath))
                                    _pyRuntime.PythonDLL = _libraryPath;
                            }
                            else
                                _pyEngine.PythonHome = _path;

                            // Detected before Initialize(): a real venv (activated normally, e.g.
                            // <venv>\Scripts\python.exe) always disables the PEP-370 user-site
                            // directory on its own, and never lets the base install's own
                            // site-packages leak in either. Our embedded interpreter never goes
                            // through that activation path, so nothing does this for us — without
                            // it, a native package installed in both the venv and the user-site (or
                            // base install) directory can resolve its Python module from one and
                            // its native DLL dependency from the other, mismatched, copy
                            // (STUD-81085). SetNoSiteFlag (Py_NoSiteFlag) disables site.main()'s
                            // automatic run entirely for that case, so neither leak path is ever
                            // added in the first place — "prevent, don't clean up after": a
                            // cleanup-after-the-fact fix couldn't undo any .pth-triggered side
                            // effects (e.g. os.add_dll_directory calls) that already ran by the
                            // time managed code regains control. `site` itself stays importable —
                            // this only skips its automatic invocation — so the explicit
                            // site.addsitedir()/site.execsitecustomize() calls in
                            // PostInitializationVenvSetup for the venv's own site-packages keep
                            // working. Deliberately not applied when venv.IncludeSystemSitePackages
                            // is true: site.main() runs normally there, which is already correct
                            // for that case.
                            var venv = _version == Version.Python_310 ? VenvDetection.GetVenvInfo(_path) : null;

                            // A venv whose declared Python version doesn't match _libraryPath means
                            // its site-packages were compiled for a different ABI — throw a clear
                            // error now rather than a confusing native failure later.
                            if (venv != null)
                                VenvDetection.ValidateVersionMatch(venv, _libraryPath);

                            if (venv != null && venv.ShouldDisableUserSite)
                                _pyEngine.SetNoSiteFlag();

                            if (_version >= Version.Python_36 && _version <= Version.Python_39)
                                _pyEngine.Initialize(null, null, null, null);
                            else
                                _pyEngine.Initialize(null, null, null);

                            ct.ThrowIfCancellationRequested();

                            PostInitializationVenvSetup(venv);

                            _pythreads = _pyEngine.BeginAllowThreads();

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
                _pyEngine.Shutdown();
                // TODO: release resources if using app domains; also clear the cache
                return Task.FromResult(true);
            }
        }

        public Task<PythonObject> LoadScript(string code, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            return RunSTA(() =>
            {
                using (_py.GIL())
                {
                    Trace.TraceInformation($"Trying to load Python script");
                    object module = null;
                    Stopwatch sw = Stopwatch.StartNew();
                    try
                    {
                        // using a Guid for "name" import
                        if (Version == Version.Python_310)
                            module = _pyModule.FromString(GetModuleName(code), code);
                        else
                            module = _pyEngine.ModuleFromString(GetModuleName(code), code);
                        var result = new PythonObject(module);
                        return result;
                    }
                    catch (TargetInvocationException e)
                    {
                        Trace.TraceError($"Python LoadScript exception: {e}");
                        ExceptionDispatchInfo.Capture(e.InnerException ?? e).Throw();
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
                using (_py.GIL())
                {
                    args = args ?? Enumerable.Empty<object>();
                    object[] paramsObj = args.Select((obj) => _toPythonMethod.Invoke(null, new object[] { obj })).ToArray();
                    Array paramsPy = Array.CreateInstance(_pyObjType, paramsObj.Length);
                    Array.Copy(paramsObj, paramsPy, paramsObj.Length);
                    Trace.TraceInformation($"Trying to execute Python method");
                    object result = null;
                    Stopwatch sw = Stopwatch.StartNew();
                    try
                    {
                        result = _pyObjInvokeMethod.Invoke(instance.PyObject, new object[] { method, paramsPy });
                    }
                    catch (TargetInvocationException e)
                    {
                        Trace.TraceError($"Python InvokeMethod exception: {e}");
                        ExceptionDispatchInfo.Capture(e.InnerException ?? e).Throw();
                    }
                    finally
                    {
                        sw.Stop();
                        Trace.TraceInformation($"Method execution took {sw.ElapsedMilliseconds} ms");
                    }
                    return new PythonObject(result);
                }
            });
        }

        public Task Execute(string code, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return RunSTA(() =>
            {
                using (_py.GIL())
                {
                    Trace.TraceInformation($"Trying to execute Python script");
                    Stopwatch sw = Stopwatch.StartNew();
                    try
                    {
                        _pyEngine.Exec(code, null, null);
                    }
                    catch (TargetInvocationException e)
                    {
                        Trace.TraceError($"Python Execute exception: {e}");
                        ExceptionDispatchInfo.Capture(e.InnerException ?? e).Throw();
                    }
                    finally
                    {
                        sw.Stop();
                        Trace.TraceInformation($"Script execution took {sw.ElapsedMilliseconds} ms");
                    }
                }

                // used as placeholder
                return true;
            });
        }

        public object Convert(PythonObject obj, Type t)
        {
            using (_py.GIL())
            {
                Trace.TraceInformation($"Trying to convert Python object to type {t}");
                return obj.AsManagedType(t);
            }
        }

        #endregion IEngine

        private void InitializeRuntime(Assembly assembly)
        {
            _pyEngine = DynamicStaticTypeMembers.Create(assembly.GetType(PythonEngineTypeName));
            _pyRuntime = DynamicStaticTypeMembers.Create(assembly.GetType(PythonRuntimeTypeName));
            _pyObject = DynamicStaticTypeMembers.Create(assembly.GetType(PythonObjectTypeName));
            _py = DynamicStaticTypeMembers.Create(assembly.GetType(PyTypeName));
            if (Version == Version.Python_310)
                _pyModule = DynamicStaticTypeMembers.Create(assembly.GetType(PythonModuleTypeName));
            _pyConverterExtension = DynamicStaticTypeMembers.Create(assembly.GetType(ConverterExtensionTypeName));

            // TODO: find a nicer way
            _pyObjType = assembly.GetType(PythonObjectTypeName);
            _pyObjInvokeMethod = _pyObjType.GetMethod(PythonObjectInvokeMethodName, new Type[] { typeof(string), _pyObjType.MakeArrayType() });
            _toPythonMethod = assembly.GetType(ConverterExtensionTypeName).GetMethod(ToPythonMethodName);
        }

        public void Dispose()
        {
            // see Release method, for the moment the runtime is cached
        }

        #region script name caching

        private Dictionary<string, string> _cachedModules = new Dictionary<string, string>();

        /// <summary>
        /// gets the module name based on the script content hash
        /// </summary>
        /// <param name="script"></param>
        /// <returns></returns>
        private string GetModuleName(string script)
        {
            string hash = Hash(script);
            string moduleName = null;
            if (_cachedModules.TryGetValue(hash, out moduleName))
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
            using (var str = asm.GetManifestResourceStream("UiPath.Python.Scripts.Init.py"))
            {
                var reader = new StreamReader(str);
                return reader.ReadToEnd();
            }
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
                // On Linux/macOS the layout is lib/pythonX.Y/site-packages
                var libPath = Path.Combine(venvPath, "lib");
                var pythonDir = Directory.GetDirectories(libPath, "python*").FirstOrDefault()
                    ?? throw new DirectoryNotFoundException($"No python* directory found under {libPath}");
                sitePackages = Path.Combine(pythonDir, "site-packages");
            }
            return sitePackages;
        }

        private void PostInitializationVenvSetup(VenvDetection.VenvInfo venv)
        {
            if (venv == null)
                return;

            using (_py.GIL())
            {
                dynamic sys = _py.Import("sys");
                dynamic site = _py.Import("site");

                // Full venv activation: sys.prefix/exec_prefix let packages locate
                // their own data files (e.g. scipy, spaCy models) inside the venv.
                // sys.base_prefix/base_exec_prefix retain the base Python location
                // and are already set correctly by Initialize().
                sys.prefix = venv.Root;
                sys.exec_prefix = venv.Root;

                // addsitedir adds site-packages to sys.path AND processes .pth files.
                // .pth processing is required for editable installs (pip install -e)
                // and packages that register extra paths via .pth (e.g. scipy, spaCy).
                var sitePackagesPath = GetEnvSitePackagesPath(venv.Root);
                site.addsitedir(sitePackagesPath);

                // addsitedir appends; move to front so venv packages take priority over base Python.
                if ((bool)sys.path.__contains__(sitePackagesPath))
                    sys.path.remove(sitePackagesPath);

                sys.path.insert(0, sitePackagesPath);

                if (venv.ShouldDisableUserSite)
                {
                    // SetNoSiteFlag (see Initialize()) skipped site.main() entirely for this
                    // default venv — which means it also skipped the plain, non-path parts of
                    // site.main() itself: setquit()/setcopyright()/sethelper(), the module-level
                    // functions that install the quit/exit/help/copyright/credits/license
                    // builtins. Without them a script calling exit() (a common RPA pattern,
                    // however discouraged) dies with a NameError that gives no hint it's related
                    // to venv handling. enablerlcompleter() is deliberately not restored — it only
                    // registers an interactive-startup readline hook, irrelevant to a
                    // non-interactive embedded script.
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
                    // --system-site-packages: SetNoSiteFlag was *not* set, so site.main() already
                    // ran during Initialize() above and may already have cached sitecustomize from
                    // whatever the base/user site resolved to sys.path first — before the venv's
                    // own site-packages, just inserted above, ever got a chance to take
                    // precedence. Only pop and re-import when the venv actually has its own copy,
                    // so it wins as intended; when it doesn't, the module already cached (already
                    // the correct, highest-priority one in that case) is left untouched, avoiding
                    // running its side effects a second time.
                    sys.modules.pop("sitecustomize", null);
                    site.execsitecustomize();
                }

                // usercustomize.py lives in the *user-site* directory, never in a venv's own
                // site-packages, and by the time we get here site.main() (when it ran, i.e. the
                // --system-site-packages case) already handled it via its own "if
                // ENABLE_USER_SITE:" guard — there is nothing left for this venv-specific setup to
                // do for it, in either case. Deliberately not calling execusercustomize()
                // ourselves, consistent with not touching user-site handling at all here.
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
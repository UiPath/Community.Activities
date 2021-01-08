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
        static extern bool SetDllDirectory(string lpPathName);

        #region Python Runtime
        /// <summary>
        /// see:
        /// https://github.com/pythonnet/pythonnet/blob/master/src/runtime/pythonengine.cs
        /// https://github.com/pythonnet/pythonnet/blob/master/src/runtime/pyobject.cs
        /// </summary>
        private const string PythonEngineTypeName = "Python.Runtime.PythonEngine";
        private const string PythonObjectTypeName = "Python.Runtime.PyObject";
        private const string PyTypeName = "Python.Runtime.Py";
        private const string ConverterExtensionTypeName = "Python.Runtime.ConverterExtension";

        private dynamic _pyEngine = null;
        private dynamic _pyObject = null;
        private dynamic _py = null;
        private dynamic _pyConverterExtension = null;
        private object _pythreads;

        // TODO: find a nicer way for method invocation
        const string PythonObjectInvokeMethodName = "InvokeMethod";
        const string ToPythonMethodName = "ToPython";

        private Type _pyObjType = null;
        private MethodInfo _toPythonMethod = null;
        private MethodInfo _pyObjInvokeMethod = null;
        #endregion

        #region Caching
        private bool _initialized = false;
        #endregion

        #region Runtime info
        private Version _version;
        private string _path;
        #endregion

        internal Engine(Version version, string path)
        {
            _version = version;
            _path = path;
        }

        #region IEngine
        public Version Version { get { return _version; } }
        public async Task Initialize(string workingFolder, CancellationToken ct)
        {
            if (!_initialized) 
            {
                lock (this)
                {
                    if (!_initialized)
                    {
                        ct.ThrowIfCancellationRequested();
                        Trace.TraceInformation($"Initializing Python runtime using version {_version} and path {_path}");
                        Stopwatch sw = Stopwatch.StartNew();

                        // needed in oder to find Python dll
                        SetDllDirectory(Path.GetFullPath(_path));

                        // load the dedicated Python.Runtime.XX.dll
                        string path = Path.GetDirectoryName(new Uri(Assembly.GetAssembly(GetType()).CodeBase).LocalPath);
                        path = Path.Combine(path, (IntPtr.Size == 8) ? "x64" : "x86");
                        path = Path.Combine(path, _version.GetAssemblyName());

                        Assembly assembly = Assembly.LoadFile(path);
                        ct.ThrowIfCancellationRequested();

                        InitializeRuntime(assembly);
                        ct.ThrowIfCancellationRequested();

                        _pyEngine.PythonHome = _path;

                        //Pythonnet removed support for version 3.3 and 3.4 so we have the old dlls. Initialize method was updated in current package. 
                        if (_version == Version.Python_33 || _version == Version.Python_34)
                            _pyEngine.Initialize(null, null);
                        else
                            if(_version == Version.Python_39)
                                _pyEngine.Initialize(null, null, null, null);
                            else
                                _pyEngine.Initialize(null, null, null);
                        ct.ThrowIfCancellationRequested();

                        _pythreads = _pyEngine.BeginAllowThreads();

                        sw.Stop();
                        Trace.TraceInformation($"Engine intialization took {sw.ElapsedMilliseconds} ms");
                        _initialized = true;
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
                        module = _pyEngine.ModuleFromString(GetModuleName(code), code);
                        var result = new PythonObject(module);
                        return result;
                    }
                    catch (TargetInvocationException e)
                    {
                        Trace.TraceError($"Python LoadScript exception: {e.ToString()}");
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
                        Trace.TraceError($"Python InvokeMethod exception: {e.ToString()}");
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
                        Trace.TraceError($"Python Execute exception: {e.ToString()}");
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
        #endregion

        private void InitializeRuntime(Assembly assembly)
        {
            _pyEngine = DynamicStaticTypeMembers.Create(assembly.GetType(PythonEngineTypeName));
            _pyObject = DynamicStaticTypeMembers.Create(assembly.GetType(PythonObjectTypeName));
            _py = DynamicStaticTypeMembers.Create(assembly.GetType(PyTypeName));
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
        #endregion

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

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            
            return tcs.Task;
        }
        #endregion
    }
}

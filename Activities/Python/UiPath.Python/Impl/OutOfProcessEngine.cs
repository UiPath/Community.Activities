using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UiPath.Python.Service;
using UiPath.Shared.Service.Client;

namespace UiPath.Python.Impl
{
    internal class OutOfProcessEngine : IEngine
    {
        private const string ServiceDll_x64 = "UiPath.Python.Host.dll";

        private PythonProxy _proxy;
        private Controller<IPythonService> _provider;

        private bool _visible;
        private readonly bool _logTrace;
        private readonly int _payloadThresholdMB;

        #region Runtime info

        private Version _version;
        private string _path;
        private string _libraryPath;

        #endregion Runtime info

        internal OutOfProcessEngine(Version version, string path, string libraryPath, TargetPlatform target, bool visible, bool logTrace, int payloadThresholdMB)
        {
            _version = version;
            _path = path;
            _libraryPath = libraryPath;
            _visible = visible;
            _logTrace = logTrace;
            _payloadThresholdMB = payloadThresholdMB;
        }

        #region IEngine

        public Version Version { get { return _version; } }

        public Task Initialize(string workingFolder, CancellationToken ct, double timeout)
        {
            ct.ThrowIfCancellationRequested();

            Trace.TraceInformation($"Initializing Python runtime using version {_version} and path {_path}");

            Stopwatch sw = Stopwatch.StartNew();

            var venv = _version == Version.Python_310 ? VenvDetection.GetVenvInfo(_path) : null;

            // Actual user-site suppression for the default (ShouldDisableUserSite) case happens
            // inside Engine.Initialize() itself, via PythonEngine.SetNoSiteFlag() — that runs in
            // this same host process regardless, so no parent-side plumbing is needed for it
            // (an env-var-based attempt used to live here; it turned out to be both unreliable
            // when set this late from managed code in an already-spawned process, and defeatable
            // by whatever PYTHONNOUSERSITE the ambient environment already carried).
            //
            // What *does* still need to happen here, in the parent, before the host spawns: for a
            // --system-site-packages venv (ShouldDisableUserSite == false), the intent is to leave
            // user-site exactly as a normal, non-embedded interpreter would — but ProcessStartInfo
            // starts as a copy of this process's own environment, so if PYTHONNOUSERSITE already
            // happens to be set there (e.g. a customer's own leftover workaround, unrelated to this
            // fix), it would otherwise leak into the host and silently force user-site off anyway,
            // regardless of what SetNoSiteFlag does or doesn't do for the other case. Clearing it
            // explicitly for the child guarantees the venv's own IncludeSystemSitePackages flag is
            // what decides this, not whatever's ambient on the machine.
            _provider = new Controller<IPythonService>()
            {
                PythonHostLibFile = ServiceDll_x64,
                Visible = _visible,
                ClearUserSiteEnvironmentOverride = venv != null && venv.IncludeSystemSitePackages
            };

            // Set LogTrace before Create() so the diagnostic file (if enabled) captures
            // host startup output too, not just lines after the service is ready.
            _provider.PythonWrapper.LogTrace = _logTrace;
            _provider.Create();
            _proxy = new PythonProxy(_provider.PythonWrapper, timeout, ct, _payloadThresholdMB);
            _proxy.Initialize(_path, _libraryPath, _version, workingFolder);

            sw.Stop();

            Trace.TraceInformation($"Engine intialization took {sw.ElapsedMilliseconds} ms");

            return Task.FromResult(true);
        }

        public Task Release()
        {
            _proxy?.Shutdown();
            _proxy = null;
            _provider?.PythonWrapper?.Dispose();//Prevent host process leak in certain scenarios
            return Task.FromResult(true);
        }

        public Task<PythonObject> LoadScript(string code, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            return Task.Run(() =>
            {
                ct.ThrowIfCancellationRequested();
                return new PythonObject(_proxy.LoadScript(code));
            }, ct);
        }

        public Task<PythonObject> InvokeMethod(PythonObject instance, string method, IEnumerable<object> args, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            return Task.Run(() =>
            {
                ct.ThrowIfCancellationRequested();
                return new PythonObject(_proxy.InvokeMethod(instance.Id, method, args.EmptyIfNull().Select(elem => new Argument(elem))));
            }, ct);
        }

        public Task Execute(string code, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            return Task.Run(() =>
            {
                ct.ThrowIfCancellationRequested();
                _proxy.Execute(code);
            }, ct);
        }

        public object Convert(PythonObject obj, Type t)
        {
            return (_proxy.Convert(obj.Id, t.FullName))?.Unwrap();
        }

        #endregion IEngine

        public void Dispose()
        {
            Release();
        }
    }
}
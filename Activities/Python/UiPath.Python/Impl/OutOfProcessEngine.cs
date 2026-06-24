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

            // TODO: expose visible as a property?
            _provider = new Controller<IPythonService>()
            {
                PythonHostLibFile = ServiceDll_x64,
                Visible = _visible
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
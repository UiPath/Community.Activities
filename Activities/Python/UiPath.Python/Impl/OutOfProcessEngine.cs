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
        const string ServiceExe_x64 = "UiPath.Python.Host.exe";
        const string ServiceExe_x86 = "UiPath.Python.Host32.exe";
        private PythonProxy _proxy;
        private Controller<IPythonService> _provider;
        private TargetPlatform _target;
        private bool _visible;

        #region Runtime info
        private Version _version;
        private string _path;     
        #endregion

        internal OutOfProcessEngine(Version version, string path, TargetPlatform target, bool visible )
        {
            _version = version;
            _path = path;
            _target = target;
            _visible = visible;
        }

        #region IEngine
        public Version Version { get { return _version; } }
        public Task Initialize(string workingFolder, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            Trace.TraceInformation($"Initializing Python runtime using version {_version} and path {_path}");

            Stopwatch sw = Stopwatch.StartNew();

            // TODO: expose visible as a property?
            _provider = new Controller<IPythonService>()
            {
                ExeFile = TargetPlatform.x64 == _target ? ServiceExe_x64: ServiceExe_x86,
                Visible = _visible
            };
            _provider.Create();
            _proxy = new PythonProxy(_provider.Endpoint);
            _proxy.Initialize(_path, _version, workingFolder); 

            sw.Stop();

            Trace.TraceInformation($"Engine intialization took {sw.ElapsedMilliseconds} ms");

            return Task.FromResult(true);
        }

        public Task Release()
        {
            _proxy?.Shutdown();
            _proxy = null;
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
            return _proxy.Convert(obj.Id, t.FullName)?.Unwrap();
        }
        #endregion

        public void Dispose()
        {
            Release();
        }
    }
}

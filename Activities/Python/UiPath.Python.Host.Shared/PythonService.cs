using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using UiPath.Python.Service;
using UiPath.Shared.Service.Host;

namespace UiPath.Python.Host
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.PerSession)]
    class PythonService : Service<IPythonService>, IPythonService
    {
        private IEngine _engine = null;
        private static readonly CancellationToken _ct = CancellationToken.None;

        private Dictionary<Guid, PythonObject> _objectCache = new Dictionary<Guid, PythonObject>();

        public Argument Convert(Guid obj, string ts)
        {
            try
            {
                object result = null;
                Type t = Type.GetType(ts);
                if (null != t && _objectCache.TryGetValue(obj, out PythonObject pyObj))
                {
                    result = _engine.Convert(pyObj, t);
                }
                return new Argument(result);
            }
            catch (Exception ex)
            {
                ExceptionDispatchInfo.Capture(ex.InnerException ?? ex).Throw();
                return null;
            }
        }

        public void Execute(string code)
        {
            try
            {
                _engine.Execute(code, _ct).Wait();
            }
            catch (Exception ex)
            {
                ExceptionDispatchInfo.Capture(ex.InnerException ?? ex).Throw();
            }
        }

        public void Initialize(Version version, string path, string workingFolder)
        {
            try
            {
                _engine = EngineProvider.Get(version, path);
                _engine.Initialize(workingFolder, _ct).Wait();
            }
            catch (Exception ex)
            {
                ExceptionDispatchInfo.Capture(ex.InnerException ?? ex).Throw();
            }
        }

        public Guid InvokeMethod(Guid instance, string method, IEnumerable<Argument> args)
        {
            try
            {
                if (!_objectCache.TryGetValue(instance, out PythonObject pyObj))
                {
                    throw new ArgumentException(nameof(instance));
                }
                PythonObject obj = _engine.InvokeMethod(pyObj, method, args.Select(elem => elem?.Unwrap()), _ct).Result;
                obj.Id = Guid.NewGuid();
                _objectCache.Add(obj.Id, obj);
                return obj.Id;
            }
            catch (Exception ex)
            {
                ExceptionDispatchInfo.Capture(ex.InnerException ?? ex).Throw();
                return Guid.Empty;
            }
        }

        public Guid LoadScript(string code)
        {
            try
            {
                PythonObject obj = _engine.LoadScript(code, _ct).Result;
                obj.Id = Guid.NewGuid();
                _objectCache.Add(obj.Id, obj);
                return obj.Id;
            }
            catch (Exception ex)
            {
                ExceptionDispatchInfo.Capture(ex.InnerException ?? ex).Throw();
                return Guid.Empty;
            }
        }

        public void Shutdown()
        {
            try
            {
                Application.Exit();
            }
            catch (Exception ex)
            {
                ExceptionDispatchInfo.Capture(ex.InnerException ?? ex).Throw();
            }
        }
    }
}

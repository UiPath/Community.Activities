using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading;
using System.Runtime.InteropServices;
using UiPath.Python.Service;
using UiPath.Shared.Service;

namespace UiPath.Python.Host
{
    internal class PythonService
    {
        #region Constants

        private static readonly Encoding _utf8Encoding = new UTF8Encoding(false);

        private const int _defaultBufferSize = 1 << 15;

        #endregion Constants

        private IEngine _engine = null;
        private static readonly CancellationToken _ct = CancellationToken.None;

        private Dictionary<Guid, PythonObject> _objectCache = new Dictionary<Guid, PythonObject>();

        private string pipeName { get; set; }

        internal NamedPipeServerStream pipeServer { get; set; }

        internal async void RunServer()
        {
            PythonResponse response = new PythonResponse
            {
                ResultState = ResultState.Successful
            };
            pipeName = Process.GetCurrentProcess().Id.ToString();
            pipeServer = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte,
                                                        PipeOptions.Asynchronous);

            pipeServer.WaitForConnection();

            using (var streamReader = new StreamReader(pipeServer, _utf8Encoding, false, _defaultBufferSize,
                                                       leaveOpen: true))
            using (var streamWriter = new StreamWriter(pipeServer, _utf8Encoding, _defaultBufferSize,
                                                           leaveOpen: true)
            { AutoFlush = true })

                while (true)
                {
                    try
                    {
                        var request = PythonRequest.Deserialize(await streamReader.ReadLineAsync());

                        switch (request.RequestType)
                        {
                            case RequestType.Initialize:
                                Initialize(request.ScriptPath, request.LibraryPath, (Version)Enum.Parse(typeof(Version), request.PythonVersion), request.WorkingFolder);
                                response.ResultState = ResultState.Successful;
                                streamWriter.WriteLine(response.Serialize());

                                break;

                            case RequestType.Shutdown:
                                response = new PythonResponse
                                {
                                    ResultState = ResultState.Successful
                                };

                                streamWriter.WriteLine(response.Serialize());
                                Shutdown();
                                break;

                            case RequestType.Execute:
                                Execute(request.Code);
                                response = new PythonResponse
                                {
                                    ResultState = ResultState.Successful
                                };
                                streamWriter.WriteLine(response.Serialize());
                                break;

                            case RequestType.LoadScript:
                                var guid = LoadScript(request.Code);
                                response = new PythonResponse
                                {
                                    ResultState = ResultState.Successful,
                                    Guid = guid
                                };

                                streamWriter.WriteLine(response.Serialize());
                                break;

                            case RequestType.InvokeMethod:
                                var guidI = InvokeMethod(request.Instance, request.Method, request.Arguments);
                                response = new PythonResponse
                                {
                                    ResultState = ResultState.Successful
                                };
                                response.Guid = guidI;
                                streamWriter.WriteLine(response.Serialize());
                                break;

                            case RequestType.Convert:
                                Argument arg = Convert(request.Instance, request.Type);
                                response = new PythonResponse
                                {
                                    Argument = arg,
                                    ResultState = ResultState.Successful
                                };

                                streamWriter.WriteLine(response.Serialize());
                                break;

                            default:
                                Shutdown();
                                break;
                        }

                        WaitForPipeDrain(pipeServer);
                    }
                    catch (Exception ex)
                    {
                        response = new PythonResponse
                        {
                            ResultState = ResultState.InstantiationException,
                        };
                        response.Errors = new List<string>();
                        response.Errors.Add(ex.Message);

                        streamWriter.WriteLine(response.Serialize());
                        WaitForPipeDrain(pipeServer);
                        throw;
                    }
                }
        }
        private bool IsWindows()
        {
            bool isWindows = true;
#if NETCOREAPP
            if(!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                isWindows = false;
#endif
            return isWindows;
        }
        private void WaitForPipeDrain(NamedPipeServerStream pipe)
        {
            if (IsWindows())
                pipe.WaitForPipeDrain();
        }

        private Argument Convert(Guid obj, string ts)
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

        private void Execute(string code)
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

        private void Initialize(string path, string libraryPath, Version version, string workingFolder)
        {
            try
            {
                _engine = EngineProvider.Get(version, path, libraryPath);
                _engine.Initialize(workingFolder, _ct).Wait();
            }
            catch (Exception ex)
            {
                ExceptionDispatchInfo.Capture(ex.InnerException ?? ex).Throw();
            }
        }

        private Guid InvokeMethod(Guid instance, string method, IEnumerable<Argument> args)
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

        private Guid LoadScript(string code)
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
                Process.GetCurrentProcess().Kill();
                Environment.Exit(1);
            }
            catch (Exception ex)
            {
                ExceptionDispatchInfo.Capture(ex.InnerException ?? ex).Throw();
            }
        }
    }
}
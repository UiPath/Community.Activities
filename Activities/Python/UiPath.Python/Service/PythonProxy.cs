using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using UiPath.Python.Properties;
using UiPath.Shared.Service;

namespace UiPath.Python.Service
{
    internal class PythonProxy : IPythonService
    {
        #region Constants

        private static readonly Encoding _utf8Encoding = new UTF8Encoding(false);

        private const int _defaultBufferSize = 1 << 15;

        #endregion Constants

        private NamedPipeClientStream ClientStream { get; set; }
        private double Timeout { get; set; }
        private CancellationToken Token { get; set; }

        internal PythonProxy(NamedPipeClientStream endpoint, double timeout, CancellationToken cancellationToken)
        {
            ClientStream = endpoint;
            Timeout = timeout;
            Token = cancellationToken;
        }

        #region Service methods

        public Argument Convert(Guid obj, string t)
        {
            using (var cts = new CancellationTokenSource((int)Timeout * 1000))
            {
                try
                {
                    var request = new PythonRequest()
                    {
                        RequestType = RequestType.Convert,
                        Instance = obj,
                        Type = t
                    };

                    PythonResponse response = RequestAsync(request, cts.Token);
                    cts.Token.ThrowIfCancellationRequested();
                    response.ThrowExceptionIfNeeded();
                    return response.Argument;
                }
                catch (Exception e)
                {
                    if (cts.IsCancellationRequested)
                    {
                        throw new TimeoutException(UiPath_Python.TimeoutException);
                    }
                    Trace.TraceError($"Python exception: {e}");
                    throw;
                }
            }
        }

        public void Execute(string code)
        {
            using (var cts = new CancellationTokenSource((int)Timeout * 1000))
            {
                try
                {
                    var request = new PythonRequest()
                    {
                        RequestType = RequestType.Execute,
                        Code = code
                    };

                    PythonResponse response = RequestAsync(request, cts.Token);
                    cts.Token.ThrowIfCancellationRequested();
                    response.ThrowExceptionIfNeeded();
                }
                catch (Exception e)
                {
                    if (cts.IsCancellationRequested)
                    {
                        throw new TimeoutException(UiPath_Python.TimeoutException);
                    }
                    Trace.TraceError($"Python exception: {e}");
                    throw;
                }
            }
        }

        public void Initialize(string path, Version version, string workingFolder)
        {
            using (var cts = new CancellationTokenSource((int)Timeout * 1000))
            {
                try
                {
                    var request = new PythonRequest()
                    {
                        RequestType = RequestType.Initialize,
                        ScriptPath = path,
                        PythonVersion = version.ToString(),
                        WorkingFolder = workingFolder
                    };

                    PythonResponse response = RequestAsync(request, Token);
                    Token.ThrowIfCancellationRequested();
                    response.ThrowExceptionIfNeeded();
                }
                catch (Exception e)
                {
                    if (cts.IsCancellationRequested)
                    {
                        throw new TimeoutException(UiPath_Python.TimeoutException);
                    }
                    Trace.TraceError($"Python exception: {e}");
                    throw;
                }
            }
        }

        public Guid InvokeMethod(Guid instance, string method, IEnumerable<Argument> args)
        {
            using (var cts = new CancellationTokenSource((int)Timeout * 1000))
            {
                try
                {
                    var request = new PythonRequest()
                    {
                        RequestType = RequestType.InvokeMethod,
                        Instance = instance,
                        Method = method,
                        Arguments = args
                    };

                    PythonResponse response = RequestAsync(request, cts.Token);
                    cts.Token.ThrowIfCancellationRequested();
                    response.ThrowExceptionIfNeeded();
                    return response.Guid;
                }
                catch (Exception e)
                {
                    if (cts.IsCancellationRequested)
                    {
                        throw new TimeoutException(UiPath_Python.TimeoutException);
                    }
                    Trace.TraceError($"Python exception: {e}");
                    throw;
                }
            }
        }

        public Guid LoadScript(string code)
        {
            using (var cts = new CancellationTokenSource((int)Timeout * 1000))
            {
                try
                {
                    var request = new PythonRequest()
                    {
                        RequestType = RequestType.LoadScript,
                        Code = code
                    };

                    PythonResponse response = RequestAsync(request, cts.Token);

                    cts.Token.ThrowIfCancellationRequested();

                    Trace.TraceInformation("LoadScript Guid:: " + response.Guid);

                    return response.Guid;
                }
                catch (Exception e)
                {
                    if (cts.IsCancellationRequested)
                    {
                        throw new TimeoutException(UiPath_Python.TimeoutException);
                    }
                    Trace.TraceError($"Python exception: {e}");
                    throw;
                }
            }
        }

        public void Shutdown()
        {
            using (var cts = new CancellationTokenSource((int)Timeout * 1000))
            {
                var request = new PythonRequest()
                {
                    RequestType = RequestType.Shutdown
                };

                PythonResponse response = RequestAsync(request, cts.Token);

                cts.Token.ThrowIfCancellationRequested();
            }
        }

        #endregion Service methods

        /// <summary>
        /// Writes the serialized Python request to the named pipe. Waits from the request to be read on Python side.
        /// Then reads the response from python(json) an deserializes it to PythonRespnse
        /// </summary>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public PythonResponse RequestAsync(PythonRequest request, CancellationToken ct)
        {
            using (CancellationTokenRegistration ctr = ct.Register(() => OnCancellationRequested()))
            {
                using (var streamWriter = new StreamWriter(ClientStream, _utf8Encoding, _defaultBufferSize,
                                                           leaveOpen: true)
                { AutoFlush = true })
                {
                    Trace.TraceInformation("Sending information to Python.");
                    streamWriter.WriteLine(request.Serialize());
                }

                ct.ThrowIfCancellationRequested();
                ClientStream.WaitForPipeDrain();

                using (var streamReader = new StreamReader(ClientStream, _utf8Encoding, false, _defaultBufferSize,
                                                           leaveOpen: true))
                {
                    return PythonResponse.Deserialize(streamReader.ReadLine());
                }
            }
        }

        #region Dispose Methods

        private void OnCancellationRequested()
        {
            OnPipeDisposed();
            OnProcessDisposed();
        }

        public void Dispose()
        {
            OnPipeDisposed();
            OnProcessDisposed();
            GC.SuppressFinalize(this);
        }

        public void OnPipeDisposed()
        {
            if (ClientStream != null && ClientStream.IsConnected)
            {
                ClientStream?.Close();
            }
            ClientStream?.Close();
            ClientStream?.Dispose();
            ClientStream = null;
        }

        public void OnProcessDisposed()
        {
        }

        ~PythonProxy()
        {
            Dispose();
        }

        #endregion Dispose Methods
    }
}
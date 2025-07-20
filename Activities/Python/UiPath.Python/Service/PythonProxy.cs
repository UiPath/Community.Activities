using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using UiPath.Python.Properties;
using UiPath.Shared.Service;
using System.Runtime.InteropServices;
using UiPath.Shared.Service.Client;

namespace UiPath.Python.Service
{
    internal class PythonProxy : IPythonService
    {
        #region Constants

        private static readonly Encoding _utf8Encoding = new UTF8Encoding(false);

        private const int _defaultBufferSize = 1 << 15;

        #endregion Constants

        private HostWrapper PythonHostWrapper { get; set; }
        private double Timeout { get; set; }
        private CancellationToken Token { get; set; }

        internal PythonProxy(HostWrapper hostWrapper, double timeout, CancellationToken cancellationToken)
        {
            PythonHostWrapper = hostWrapper;
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

        public void Initialize(string path, string libraryPath, Version version, string workingFolder)
        {
            using (var cts = new CancellationTokenSource((int)Timeout * 1000))
            {
                try
                {
                    var request = new PythonRequest()
                    {
                        RequestType = RequestType.Initialize,
                        ScriptPath = path,
                        LibraryPath = libraryPath,
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
                    response.ThrowExceptionIfNeeded();
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
            //Process has already exited, no need to send kill request
            if (PythonHostWrapper.HostProcessHasExited())
                return;

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
                SendRequest(request, ct);
                return ReadResponse(request, ct);
            }
        }

        private void SendRequest(PythonRequest request, CancellationToken ct)
        {
            try
            {
                using (var streamWriter = new StreamWriter(PythonHostWrapper.Pipe, _utf8Encoding, _defaultBufferSize,
                                                           leaveOpen: true)
                { AutoFlush = true })
                {
                    Trace.TraceInformation("Sending information to Python.");
                    streamWriter.WriteLine(request.Serialize());
                }
                ct.ThrowIfCancellationRequested();
                WaitForPipeDrain();
            }
            catch
            {
                //ignore exception here
                //the exception will be thrown later
                //if we rethrow the exception, we might get generic error here
            }

            ct.ThrowIfCancellationRequested();
            PythonHostWrapper.ThrowIfProcessHasExited();
        }

        private PythonResponse ReadResponse(PythonRequest request, CancellationToken ct)
        {
            PythonResponse response = null;
            try
            {
                using (var streamReader = new StreamReader(PythonHostWrapper.Pipe, _utf8Encoding, false, _defaultBufferSize,
                                                           leaveOpen: true))
                {
                    response = PythonResponse.Deserialize(streamReader.ReadLine());
                }
            }
            catch
            {
                //ignore exception, see above
            }

            ct.ThrowIfCancellationRequested();
            PythonHostWrapper.ThrowIfProcessHasExited();
            return response;
        }

        private bool IsWindows()
        {
            bool isWindows = true;
#if NETCOREAPP
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                isWindows = false;
#endif
            return isWindows;
        }
        private void WaitForPipeDrain()
        {
            if (IsWindows())
                PythonHostWrapper.Pipe.WaitForPipeDrain();
        }

        #region Dispose Methods

        private void OnCancellationRequested()
        {
            DisposeHostWrapper();
        }

        public void Dispose()
        {
            DisposeHostWrapper();
            GC.SuppressFinalize(this);
        }

        private void DisposeHostWrapper()
        {
            PythonHostWrapper?.Dispose();
        }

        ~PythonProxy()
        {
            Dispose();
        }

        #endregion Dispose Methods
    }
}
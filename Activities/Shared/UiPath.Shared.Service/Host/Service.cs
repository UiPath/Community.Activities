using System;
using System.Diagnostics;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.Threading;

namespace UiPath.Shared.Service.Host
{
    /// <summary>
    /// Simpe WCF server, using named pipes, with name is based on process id
    /// </summary>
    /// <typeparam name="T">WCF service contract/interface</typeparam>
    internal abstract class Service<T> : IDisposable
    {
        protected ServiceHost _service = null;

        protected Mutex _sync = null;

        protected TimeSpan SendTimeout { get; set; } = Config.DefaultSendTimeout;

        protected TimeSpan ReceiveTimeout { get; set; } = Config.DefaultReceiveTimeout;

        protected bool IncludeExceptionsInFaults { get; set; }

        internal Service(bool includeExceptionsInFaults = true)
        {
            IncludeExceptionsInFaults = includeExceptionsInFaults;
        }

        internal void Start()
        {
            string serviceAddress = Config.MakeServiceAddress(typeof(T), Process.GetCurrentProcess().Id);
            Trace.TraceInformation($"Starting WCF service {serviceAddress}");

            try
            {
                // create the service
                _service = new ServiceHost(GetType());

                // create pipe binding
                NetNamedPipeBinding pipeBinding = new NetNamedPipeBinding(NetNamedPipeSecurityMode.None)
                {
                    ReceiveTimeout = ReceiveTimeout,
                    SendTimeout = SendTimeout,
                    MaxBufferSize = int.MaxValue,
                    MaxReceivedMessageSize = int.MaxValue
                };
                _service.AddServiceEndpoint(typeof(T), pipeBinding, serviceAddress);

                // Metadata
                ServiceMetadataBehavior smb = _service.Description.Behaviors.Find<ServiceMetadataBehavior>() ?? new ServiceMetadataBehavior();
                _service.Description.Behaviors.Add(smb);
                if(IncludeExceptionsInFaults)
                {
                    _service.Description.Behaviors.Remove(typeof(ServiceDebugBehavior));
                    _service.Description.Behaviors.Add(new ServiceDebugBehavior { IncludeExceptionDetailInFaults = true });
                }

                // Add MEX endpoint
                _service.AddServiceEndpoint(ServiceMetadataBehavior.MexContractName, MetadataExchangeBindings.CreateMexNamedPipeBinding(),
                    serviceAddress + "/mex");

                // Open
                _service.Open();

                // signal the service is ready
                _sync = new Mutex(false, serviceAddress);

                Trace.TraceInformation($"WCF service {serviceAddress} started successfully");

            }
            catch (Exception e)
            {
                Trace.TraceError($"Error starting WCF service: {e.ToString()}");
                throw;
            }
        }

        internal void Stop()
        {
            _service?.Close();
            _service = null;
            _sync?.Close();
            _sync = null;
        }

        public void Dispose()
        {
            Stop();
        }
    }
}

using System;
using System.Diagnostics;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace UiPath.Shared.Service.Client
{
    /// <summary>
    /// WCF client, handling creation/reconnection
    /// </summary>
    /// <typeparam name="T">WCF service contract/interface</typeparam>
    internal abstract class Proxy<T> : IDisposable where T : class
    {
        #region service connection
        protected object _lock = new object();
        protected T _channel = default(T);
        protected ChannelFactory<T> _factory = null;

        protected TimeSpan OperationTimeout { get; set; } = Config.DefaultOperationTimeout;
        #endregion

        internal Proxy(string endpoint, Binding binding = null)
        {
            Initialize(endpoint, binding ?? new NetNamedPipeBinding(NetNamedPipeSecurityMode.None) { MaxReceivedMessageSize = 2147483647 });
        }

        protected virtual void Initialize(string endpoint, Binding binding)
        {
            _factory = new ChannelFactory<T>(binding, new EndpointAddress(endpoint));
            CreateChannel();
        }

        protected virtual void Release()
        {
            ReleaseChannel();
            _factory?.Close();
            _factory = null;
        }

        protected virtual void CreateChannel()
        {
            lock (_lock)
            {
                try
                {
                    _channel = _factory.CreateChannel();
                    (_channel as ICommunicationObject).Faulted += Channel_Faulted;
                    (_channel as IContextChannel).OperationTimeout = OperationTimeout;
                }
                catch (Exception e)
                {
                    Trace.TraceError($"Error creating channel: {e.ToString()}");
                    throw;
                }
            }
        }

        protected virtual void ReleaseChannel()
        {
            lock (_lock)
            {
                try
                {
                    ICommunicationObject commObj = _channel as ICommunicationObject;
                    if (null != commObj)
                    {
                        commObj.Faulted -= Channel_Faulted;
                        commObj.Abort();
                    }
                }
                catch (Exception e)
                {
                    Trace.TraceError($"Error aborting channel: {e.ToString()}");
                }
                finally
                {
                    _channel = null;
                }
            }
        }

        protected void Channel_Faulted(object sender, EventArgs e)
        {
            lock (_lock)
            {
                // try to recreate
                ReleaseChannel();
                CreateChannel();
            }
        }

        public void Dispose()
        {
            Release();
        }
    }
}

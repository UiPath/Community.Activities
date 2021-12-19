using System;
using System.Activities;
using System.Threading;
using System.Threading.Tasks;
using System.Activities.Statements;
using System.ComponentModel;
using UiPath.WebhookHandler.Activities.Properties;
using System.Net;
using System.Text;
using UiPath.WebhookHandler.Contracts;
using UiPath.Shared.Activities;

namespace UiPath.WebhookHandler.Activities
{
    class WebhookConfig
    {
        public bool Run { get; set; } = true;
        public string Path { get; set; }
        public string Method { get; set; } = "POST";
    }

    [LocalizedDisplayName(nameof(Resources.WebhookHandler_DisplayName))]
    [LocalizedDescription(nameof(Resources.WebhookHandler_Description))]
    public class WebhookHandler : ContinuableAsyncNativeActivity
    {
        #region Properties

        [Browsable(false)]
        public ActivityAction<IObjectContainer​> Body { get; set; }

        /// <summary>
        /// If set, continue executing the remaining activities even if the current activity has failed.
        /// </summary>
        [LocalizedCategory(nameof(Resources.Common_Category))]
        [LocalizedDisplayName(nameof(Resources.ContinueOnError_DisplayName))]
        [LocalizedDescription(nameof(Resources.ContinueOnError_Description))]
        public override InArgument<bool> ContinueOnError { get; set; }


        [LocalizedDisplayName(nameof(Resources.Handler_IpcChannel_DisplayName))]
        [LocalizedDescription(nameof(Resources.Handler_IpcChannel_Description))]
        [RequiredArgument]
        public InArgument<string> IpcChannel { get; set; }
        [LocalizedDisplayName(nameof(Resources.Handler_Endpoint_DisplayName))]
        [LocalizedDescription(nameof(Resources.Handler_Endpoint_Description))]
        [LocalizedCategory(nameof(Resources.Input_Category))]
        [RequiredArgument]
        public InArgument<string> Endpoint { get; set; }
        [LocalizedDisplayName(nameof(Resources.Handler_RoutePath_DisplayName))]
        [LocalizedDescription(nameof(Resources.Handler_RoutePath_Description))]
        [LocalizedCategory(nameof(Resources.Input_Category))]
        [RequiredArgument]
        public InArgument<string> RoutePath { get; set; }

        // A tag used to identify the scope in the activity context
        internal static string ParentContainerPropertyTag => "ScopeActivity";

        // Object Container: Add strongly-typed objects here and they will be available in the scope's child activities.
        private readonly IObjectContainer _objectContainer;

        private static string channel;
        private HttpListener listener;
        private Thread handler;
        private WebhookConfig config = new WebhookConfig(); 
        #endregion


        #region Constructors

        public WebhookHandler(IObjectContainer objectContainer)
        {
            _objectContainer = objectContainer;

            Body = new ActivityAction<IObjectContainer>
            {
                Argument = new DelegateInArgument<IObjectContainer> (ParentContainerPropertyTag),
                Handler = new Sequence { DisplayName = Resources.Do }
            };
        }

        public WebhookHandler() : this(new ObjectContainer())
        {

        }

        #endregion


        #region Protected Methods

        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            if (IpcChannel == null) metadata.AddValidationError(string.Format(Resources.ValidationValue_Error, nameof(IpcChannel)));
            if (Endpoint == null) metadata.AddValidationError(string.Format(Resources.ValidationValue_Error, nameof(Endpoint)));
            if (RoutePath == null) metadata.AddValidationError(string.Format(Resources.ValidationValue_Error, nameof(RoutePath)));

            base.CacheMetadata(metadata);
        }

        protected override async Task<Action<NativeActivityContext>> ExecuteAsync(NativeActivityContext  context, CancellationToken cancellationToken)
        {
            // Inputs
            channel = IpcChannel.Get(context);
            var endpoint = Endpoint.Get(context);
            var routepath = RoutePath.Get(context);
            config.Run = true;
            config.Path = routepath;

            if (!string.IsNullOrEmpty(channel))
                channel = channel.Replace("/", @"\");

            listener = new HttpListener();
            listener.Prefixes.Add(endpoint);
            listener.Start();
            handler = new Thread(new ParameterizedThreadStart(HandleIncomingConnections));
            handler.Start( config);
            return (ctx) => {
                // Schedule child activities
                if (Body != null)
				    ctx.ScheduleAction<IObjectContainer>(Body, _objectContainer, OnCompleted, OnFaulted);

                // Outputs
            };
        }

        private async void HandleIncomingConnections( object cfg)
        {
            WebhookConfig config = (WebhookConfig)cfg;

            // While a user hasn't visited the `shutdown` url, keep on handling requests
            while (config.Run)
            {
                // Will wait here until we hear from a connection
                HttpListenerContext ctx = await listener.GetContextAsync();

                // Peel out the requests and response objects
                HttpListenerRequest req = ctx.Request;
                HttpListenerResponse resp = ctx.Response;

                // Print out some info about the request
#if DEBUG
                Console.WriteLine(req.Url.ToString());
                Console.WriteLine(req.HttpMethod);
                Console.WriteLine(req.UserHostName);
                Console.WriteLine(req.UserAgent);
#endif

                if ((req.HttpMethod == config.Method) && req.Url.AbsolutePath == config.Path)
                {
                    byte [] buf = new byte[req.ContentLength64];

                    await req.InputStream.ReadAsync(buf, 0, buf.Length);
                    
                    byte[] data = Encoding.UTF8.GetBytes("webhook response data");
                    resp.ContentType = "text/plain";
                    resp.ContentEncoding = Encoding.UTF8;
                    resp.ContentLength64 = data.LongLength;

                    // Write out to the response stream (asynchronously), then close it
                    await resp.OutputStream.WriteAsync(data, 0, data.Length);
                    resp.Close();

                    UiPath.IPC.Activities.BroadcastMessage broadcastMessage = new UiPath.IPC.Activities.BroadcastMessage();
                    broadcastMessage.Channel = channel;
                    broadcastMessage.Message = Encoding.UTF8.GetString(buf);
                    broadcastMessage.ContinueOnError = false;
                    broadcastMessage.PollingInterval = 100;
                    broadcastMessage.Timeout = 1000;
                    WorkflowInvoker.Invoke(broadcastMessage);
                }
            }
        }
        #endregion


        #region Events

        private void OnFaulted(NativeActivityFaultContext faultContext, Exception propagatedException, ActivityInstance propagatedFrom)
        {
            faultContext.CancelChildren();
            Cleanup();
        }

        private void OnCompleted(NativeActivityContext context, ActivityInstance completedInstance)
        {

            Cleanup();
        }

        #endregion


        #region Helpers
        
        private void Cleanup()
        {
            var disposableObjects = _objectContainer.Where(o => o is IDisposable);
            foreach (var obj in disposableObjects)
            {
                if (obj is IDisposable dispObject)
                    dispObject.Dispose();
            }
            _objectContainer.Clear();
            config.Run = false;
            handler.Join();
        }

        #endregion
    }
}


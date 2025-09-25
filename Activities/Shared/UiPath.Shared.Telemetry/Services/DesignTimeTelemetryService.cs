//using System.Collections.Generic;
//using TelemetryClient.Contracts;
//using TelemetryClient.Contracts.Outlets;
//using TelemetryClient.Implementations.Configuration.Model;
//using TelemetryClient.Implementations.Serialization;

//namespace UiPath.Shared.Telemetry.Services
//{
//    /// <summary>
//    /// Used to capture telemetry events only at design time
//    /// Does not need any reference to Platform or IWorkflowRuntime
//    /// </summary>
//    internal class DesignTimeTelemetryService : TelemetryServiceBase
//    {
//        private static ITelemetryClient s_client;

//        /// <summary>
//        /// If true, telemetry is enabled
//        /// </summary>
//        internal static bool IsSupported { get; set; }

//        private static ITelemetryClient Client
//        {
//            get
//            {
//                if (!IsSupported)
//                {
//                    return null;
//                }

//                if (s_client == null)
//                {
//                    s_client = CreateTelemetryClient();
//                }

//                return s_client;
//            }
//        }

//        protected override void TrackEventInternal(BasicEvent basicEvent)
//        {
//            try
//            {
//                Client?.TrackEvent(basicEvent);
//            }
//            catch
//            {
//                // do not trace
//            }
//        }

//        public override bool IsEnabled => IsSupported;

//        protected override IOperation TrackOperationInternal(BasicOperation basicOperation)
//        {
//            return Client?.StartOperation(basicOperation);
//        }

//        protected override IOperation TrackExecutionOperationInternal(ExecutionOperation data)
//        {
//            // Used for activity execution, can't be used at design time.
//            return null;
//        }

//        private static ITelemetryClient CreateTelemetryClient()
//        {
//            var config = new TelemetryConfiguration
//            {
//                IsEnabled = true,
//                ApplicationName = TelemetryCommon.Assembly,
//                Outlets = new List<OutletConfiguration>
//                        {
//                            new DesignTimeTelemetryOutletConfiguration()
//                            {
//                                Name = TelemetryCommon.UiPathProxy,
//                            }
//                        }
//            };

//            var container = new TelemetryConfigurationContainer
//            {
//                Name = TelemetryCommon.DefaultContainer,
//                DefaultEventOutlet = TelemetryCommon.UiPathProxy,
//                DefaultOperationOutlet = TelemetryCommon.UiPathProxy,
//            };

//            config.Containers.Add(container);
//            return new TelemetryClientBuilder()
//                .WithConfig(config)
//                .Build();
//        }
//    }

//    [OutletType(typeof(DesignTimeTelemetryOutlet))]
//    internal class DesignTimeTelemetryOutletConfiguration : OutletConfiguration
//    {
//    }

//    internal class DesignTimeTelemetryOutlet : IOutlet
//    {
//        private readonly TelemetrySerializer _telemetrySerializer = new TelemetrySerializer();
//        private readonly ITelemetryProxy _designTelemetryProxy;

//        public string Name => nameof(DesignTimeTelemetryOutlet);

//        public Outlet<IOperation> PushOperation => Export;

//        public Outlet<IEvent> PushEvent => Export;

//        public DesignTimeTelemetryOutlet()
//        {
//            _designTelemetryProxy = new LegacyDesignerContract().TryInvoke(c => c.UnSafe().TelemetryProxy);
//        }

//        // All custom outlets must have a constructor with an OutletConfiguration.
//        public DesignTimeTelemetryOutlet(DesignTimeTelemetryOutletConfiguration config)
//            : this()
//        {
//        }

//        public void Export(ITelemetry telemetry)
//        {
//            var serializedTelemetry = _telemetrySerializer.Serialize(telemetry);
//            _designTelemetryProxy?.Send(serializedTelemetry);
//        }
//    }
//}

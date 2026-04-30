using System.Collections.Generic;
using TelemetryClient.Contracts;
using TelemetryClient.Implementations.Configuration.Model;
using UiPath.Shared.Telemetry.Contracts;

namespace UiPath.Shared.Telemetry
{
    internal static class TelemetryProvider
    {
        private static ITelemetryClient s_client;
        private static TelemetryOutletConfiguration s_telemetryOutletConfiguration;

        public static bool IsSupported { get; set; }

        static TelemetryProvider()
        {
            try
            {
                // check runtime, when started by the robot
                IsSupported = true;
            }
            catch
            {
                // do not trace exception
                IsSupported = false;
            }
        }

        internal static TelemetryOutletConfiguration TelemetryOutletConfiguration
        {
            get
            {
                if (s_telemetryOutletConfiguration == null)
                {
                    s_telemetryOutletConfiguration = new TelemetryOutletConfiguration
                    {
                        Name = TelemetryCommon.UiPathProxy,
                    };
                }

                return s_telemetryOutletConfiguration;
            }
        }

        internal static ITelemetryClient Client
        {
            get
            {
                if (IsSupported && s_client == null)
                {
                    s_client = GetTelemetryClient();
                }

                return s_client;
            }
        }

        internal static ITelemetryClient GetTelemetryClient()
        {
            var config = new TelemetryConfiguration
            {
                IsEnabled = true,
                ApplicationName = TelemetryCommon.Assembly,
                Outlets = new List<OutletConfiguration>
                        {
                            TelemetryOutletConfiguration
                        }
            };

            var container = new TelemetryConfigurationContainer
            {
                Name = TelemetryCommon.DefaultContainer,
                DefaultEventOutlet = TelemetryCommon.UiPathProxy,
                DefaultOperationOutlet = TelemetryCommon.UiPathProxy,
            };

            config.Containers.Add(container);
            return new TelemetryClientBuilder()
                .WithConfig(config)
                .Build();
        }
    }
}
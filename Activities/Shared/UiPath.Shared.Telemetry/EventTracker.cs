using System;
using System.Diagnostics;
using TelemetryClient.Contracts;
using TelemetryClient.Contracts.Model;

namespace UiPath.Shared.Telemetry
{
    internal static class EventTracker
    {
        private static readonly ITelemetryClient s_telemetryClient = TelemetryProvider.GetTelemetryClient();

        public static void Track(TelemetryEvent telemetryEvent)
        {
            try
            {
                s_telemetryClient.TrackEvent(telemetryEvent);
            }
            catch (Exception e)
            {
                Trace.TraceError(e.ToString());
            }
        }
    }
}

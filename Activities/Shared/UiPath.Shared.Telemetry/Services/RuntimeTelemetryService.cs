using System;
using System.Activities;
using System.Linq;
using System.Reflection;
using UiPath.Shared.Activities;
using UiPath.Shared.Telemetry.Contracts;

namespace UiPath.Shared.Telemetry.Services
{
    internal class RuntimeTelemetryService : TelemetryService
    {
        private static IActivityTelemetryHandler s_activityTelemetryHandler;

        public static ITelemetryOperationWrapper CreateExecutionOperation(Activity activity, ActivityContext context, string eventName = null)
        {
            var telemetryHandler = GetActivityTelemetryHandler();
            var service = telemetryHandler?.GetRuntimeTelemetryService();
            if (service == null || !service.IsEnabled)
                return null;

            var operation = telemetryHandler.CreateExecutionOperation(activity, context, eventName);
            if (operation == null)
                return null;

            var iOperation = service.TrackExecutionOperation(operation);
            return new TelemetryOperationWrapper(telemetryHandler, operation, iOperation);
        }

        internal static IActivityTelemetryHandler GetActivityTelemetryHandler() =>
            s_activityTelemetryHandler ?? (s_activityTelemetryHandler = CreateActivityTelemetryHandler());

        private static IActivityTelemetryHandler CreateActivityTelemetryHandler()
        {
            // get a hold of our assembly
            var assembly = typeof(RuntimeTelemetryService).Assembly;

            // search the type for our IActivityTelemetryHandler
            var refType = GetActivityTelemetryHandlerTypeByAssemblyAttribute(assembly) ?? typeof(DefaultActivityTelemetryHandler);

            try
            {
                return Activator.CreateInstance(refType) as IActivityTelemetryHandler;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError(ex.ToString());
                return null;
            }
        }

        /// <summary>
        /// Gets the Type to be used as ActivityTelemetryHandler by searching for an AssemblyMetadataAttribute with RuntimeActivityTelemetryHandler key.
        /// </summary>
        /// <param name="assembly"></param>
        /// <returns></returns>
        private static Type GetActivityTelemetryHandlerTypeByAssemblyAttribute(Assembly assembly)
        {
            try
            {
                var attribute = assembly.GetCustomAttributes<AssemblyMetadataAttribute>().FirstOrDefault(a => a.Key == "RuntimeActivityTelemetryHandler");
                return attribute == null ? null : assembly.GetType(attribute.Value);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError(ex.ToString());
                return null;
            }
        }
    }
}

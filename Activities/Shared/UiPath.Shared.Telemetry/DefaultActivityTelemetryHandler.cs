using System;
using System.Activities;
using UiPath.Shared.Telemetry.Contracts;
using UiPath.Shared.Telemetry.Services;

namespace UiPath.Shared.Telemetry
{
    internal class DefaultActivityTelemetryHandler : IActivityTelemetryHandler
    {
        private ITelemetryService _runtimeTelemetryService;
        private string _packageInferredFromActivity;
        private string _packageVersionFromActivity;

        public virtual string Package => _packageInferredFromActivity;

        public virtual string Version => _packageVersionFromActivity;

        public virtual string GetActivityEventName(Activity activity)
        {
            var activityType = GetActivityType(activity);

            // initialize Package & Version if not set
            if (Package == null || Version == null)
            {
                var assemblyName = activityType.Assembly.GetName();
                _packageInferredFromActivity = assemblyName.Name;
                _packageVersionFromActivity = assemblyName.Version.ToString();
            }

            return $"{Package}.{RemoveGenericTypeCount(activityType.Name)}";
        }

        public virtual ExecutionOperation CreateExecutionOperation(Activity activity, ActivityContext context, string eventName = null)
        {
            // First, set the package and version in case it is null
            var activityName = GetActivityEventName(activity);
            if (activityName == null && eventName == null)
                return null;

            if (activity is ICustomTelemetryActivity customTelemetryActivity)
            {
                return customTelemetryActivity.GetExecutionOperationData(this, context);
            }

            return new ExecutionOperation(GetFullName(GetActivityType(activity)), Package, Version, eventName ?? activityName);
        }

        public virtual ITelemetryService GetRuntimeTelemetryService() =>
            _runtimeTelemetryService ??= new RuntimeTelemetryService();

        public virtual void PreProcessException(ActivityExceptionData exceptionData)
        {
            // Nothing to do
        }

        /// <summary>
        /// Similar to <see cref="Type.FullName"/>, without any generic details <br/>
        /// Note: Using <see cref="Type.FullName"/> for generic types comes with undesired trailing generic details, eg. "[[System.Int32, System.Private.CoreLib, Version=6.0.0.0,Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]"
        /// </summary>
        /// <returns></returns>
        private static string GetFullName(Type type) => $"{type.Namespace}.{RemoveGenericTypeCount(type.Name)}";

        /// <summary>
        /// Remove ending generic parameter type count, if the type is generic.
        /// <br />Input -> GenericActivity`2, output -> GenericActivity (removes ending `1, `2)
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private static string RemoveGenericTypeCount(string name)
        {
            if (name.Length > 3 && name[name.Length - 2] == '`')
                return name.Substring(0, name.Length - 2);

            return name;
        }

        /// <summary>
        /// Gets the actual activity type, considering <see cref="ITelemetryAliasActivity"/> if implemented.
        /// </summary>
        /// <param name="activity"></param>
        /// <returns></returns>
        private static Type GetActivityType(Activity activity)
        {
            Type type = null;
            if (activity is ITelemetryAliasActivity asAliasActivity)
                type = asAliasActivity.TelemetryActivityType;

            return type ?? activity.GetType();
        }
    }
}

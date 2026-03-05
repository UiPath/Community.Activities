using Newtonsoft.Json;
using System.Activities;
using System.Collections.Generic;
using UiPath.Shared.Telemetry.Contracts;

namespace UiPath.Shared.Telemetry
{
    /// <summary>
    /// Contains the basic telemetry properties that should be reported for activity executions.
    /// In order to report additional information, derive from this class and add properties.
    /// </summary>
    internal class ExecutionOperation : BasicOperation, IActivityTelemetryData
    {
        /// <summary>
        /// The operation name should be [PackageName].[ActivityName].
        /// Provide a name if the assembly name is different from the package name
        /// or the activity class name is different from the activity name.
        /// </summary>
        public ExecutionOperation(Activity caller, ActivityContext context, string name = null) : base(name ?? caller.GetType().FullName)
        {
            var assemblyName = caller.GetType().Assembly.GetName();

            ActivityType = Name;
            ActivityPackage = assemblyName.Name;
            ActivityPackageVersion = assemblyName.Version.ToString();
        }

        public ExecutionOperation(string activityType, string activityPackage, string activityPackageVersion, string eventName = null) : base(eventName ?? activityType)
        {
            ActivityType = activityType;
            ActivityPackage = activityPackage;
            ActivityPackageVersion = activityPackageVersion;
        }

        public string ActivityType { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string Error { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public object Data { get; set; }
    }

    internal class ProfilingData
    {
        public string ActivityId { get; set; }

        public string WorkflowInstanceId { get; set; }

        public string WorkflowFilePath { get; set; }

        public Dictionary<string, object> Data { get; } = new Dictionary<string, object>();
    }
}

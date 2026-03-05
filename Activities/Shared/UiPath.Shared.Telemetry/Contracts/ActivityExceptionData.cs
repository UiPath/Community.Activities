using System;
using Newtonsoft.Json;

namespace UiPath.Shared.Telemetry.Contracts
{
    internal class ActivityExceptionData : BasicEvent, IActivityTelemetryData
    {
        private const int TelemetryMaxEventLength = 7000;

        private ActivityExceptionData(string name) : base(name)
        { }

        public string Error => Exception.GetType().FullName;

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string StackTrace => Truncate(Exception.StackTrace, TelemetryMaxEventLength - (Message?.Length ?? 0));

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string Message { get; set; }

        public string ActivityType { get; private set; }

        [JsonIgnore]
        public Exception Exception { get; private set; }

        public static ActivityExceptionData From(string eventName, IActivityTelemetryData activityData, Exception exception) =>
            new ActivityExceptionData(eventName)
            {
                Exception = exception,
                Message = exception.Message,
                ActivityType = activityData.ActivityType,
                ActivityPackage = activityData.ActivityPackage,
                ActivityPackageVersion = activityData.ActivityPackageVersion,
            };

        private static string Truncate(string input, int maxLength)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            return input.Length < maxLength ? input : input.Substring(0, maxLength);
        }
    }
}

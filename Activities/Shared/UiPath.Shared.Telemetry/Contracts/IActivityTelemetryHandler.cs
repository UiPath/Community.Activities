using System.Activities;

namespace UiPath.Shared.Telemetry.Contracts
{
    internal interface IActivityTelemetryHandler
    {
        /// <summary>
        /// Package name.
        /// </summary>
        string Package { get; }

        /// <summary>
        /// Package version.
        /// </summary>
        string Version { get; }

        /// <summary>
        /// Gets the event name to be used for the given activity.
        /// <br />Method should return a value {Package_Name}.{Activity_Name}.
        /// </summary>
        /// <param name="activity"></param>
        /// <returns></returns>
        string GetActivityEventName(Activity activity);

        /// <summary>
        /// Creates a <see cref="ExecutionOperation"/> based on the given activity that is executing in the indicated context.
        /// </summary>
        /// <param name="activity"></param>
        /// <param name="context"></param>
        /// <param name="eventName">Allows overriding the default eventName, which is the Activity Name.</param>
        /// <returns></returns>
        ExecutionOperation CreateExecutionOperation(Activity activity, ActivityContext context, string eventName = null);

        /// <summary>
        /// Gets a <see cref="ITelemetryService"/> to be used at runtime to send telemetry.
        /// </summary>
        /// <returns></returns>
        ITelemetryService GetRuntimeTelemetryService();

        /// <summary>
        /// Called before the given exception data is sent to telemetry.
        /// <br />Allows activity handlers to pre-process the data to be sent.
        /// <br />Usually, handlers should clear the <see cref="ActivityExceptionData.Message"/> property is the exception type is known with static content, or if it can contain PII (private/sensitive) data.
        /// </summary>
        /// <param name="exceptionData"></param>
        void PreProcessException(ActivityExceptionData exceptionData);
    }
}

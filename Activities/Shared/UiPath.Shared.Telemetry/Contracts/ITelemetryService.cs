using TelemetryClient.Contracts;

namespace UiPath.Shared.Telemetry.Contracts
{
    internal interface ITelemetryService
    {
        /// <summary>
        /// Indicates whether telemetry is enabled.
        /// </summary>
        bool IsEnabled { get; }

        /// <summary>
        /// Tracks a single telemetry event.
        /// If an operation is open when the event is launched, it is added as the operation's child.
        /// </summary>
        /// <param name="basicEvent">Event to be sent.</param>
        void Track(BasicEvent basicEvent);

        /// <summary>
        /// The data is sent when the returned <see cref="IOperation"/> is disposed.
        /// The <see cref="IOperation.Success"/> flag should be set before disposing the operation.
        /// Duration is computed by registering timestamps when the operation is created and disposed,
        /// so it should be created as soon as execution starts and disposed after it is completed.
        /// Any other operation or event launched while this operation is open will be added as its child. 
        /// </summary>
        /// <param name="basicOperation"></param>
        /// <returns></returns>
        IOperation Track(BasicOperation basicOperation);

        /// <summary>
        /// Wraps the given event data into a <see cref="BasicEvent"/> with the given name and send a single telemetry event.
        /// If an operation is open when the event is launched, it is added as the operation's child.
        /// </summary>
        /// <param name="data">Custom data to be reported.</param>
        /// <param name="name">The operation name, visible in Insights.</param>
        void TrackEvent<T>(T data, string name = null);

        /// <summary>
        /// The data is sent when the returned <see cref="IOperation"/> is disposed.
        /// The <see cref="IOperation.Success"/> flag should be set before disposing the operation.
        /// Duration is computed by registering timestamps when the operation is created and disposed,
        /// so it should be created as soon as execution starts and disposed after it is completed.
        /// Any other operation or event launched while this operation is open will be added as its child. 
        /// </summary>
        /// <param name="data">Custom data to be reported.</param>
        /// <param name="name">The operation name, visible in Insights.</param>
        /// <returns>A disposable operation handle.</returns>
        IOperation TrackOperation<T>(T data, string name = null);

        /// <summary>
        /// Tracks mandatory data for an activity execution.
        /// See <see cref="TrackOperation{T}(T, string)"/> for usage details.
        /// </summary>
        /// <param name="data">The telemetry data to be reported.</param>
        /// <returns>A disposable operation handle.</returns>
        IOperation TrackExecutionOperation(ExecutionOperation data);
    }
}

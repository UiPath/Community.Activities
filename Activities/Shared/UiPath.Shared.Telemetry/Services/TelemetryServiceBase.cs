using TelemetryClient.Contracts;

namespace UiPath.Shared.Telemetry.Services
{
    internal abstract class TelemetryServiceBase : Contracts.ITelemetryService
    {
        private const string TagEvent = "Event";

        public abstract bool IsEnabled { get; }

        public void Track(BasicEvent basicEvent) => TrackEventInternal(basicEvent);

        /// <summary>
        /// Track event with name generated based on the <typeparamref name="T"/> type name, without ending "Event", eg. IndicateCardEvent => IndicateCard <br />
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data"></param>
        /// <param name="name"></param>
        public void TrackEvent<T>(T data, string name = null) => TrackEventInternal(new BasicEvent<T>(name ?? GetEventName(data), data));

        public IOperation Track(BasicOperation basicOperation) => TrackOperationInternal(basicOperation);

        public IOperation TrackOperation<T>(T data, string name = null) => TrackOperationInternal(new BasicOperation<T>(name ?? GetOperationName(data), data));

        public IOperation TrackExecutionOperation(ExecutionOperation data) => TrackExecutionOperationInternal(data);

        private string GetOperationName<T>(T data)
        {
            return data.GetType().Name;
        }

        private string GetEventName<T>(T data) => GetEventName(data.GetType().Name);

        private string GetEventName(string typeName) =>
            typeName.EndsWith(TagEvent) ? typeName.Remove(typeName.Length - TagEvent.Length) : typeName;

        protected abstract IOperation TrackOperationInternal(BasicOperation operation);

        protected abstract IOperation TrackExecutionOperationInternal(ExecutionOperation data);

        protected abstract void TrackEventInternal(BasicEvent basicEvent);
    }
}

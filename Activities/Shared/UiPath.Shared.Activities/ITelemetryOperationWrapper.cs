namespace UiPath.Shared.Activities
{
    /// <summary>
    /// Taken from System Activities
    /// </summary>
    internal interface ITelemetryOperationWrapper
    {
        void SendWithException(Exception ex);

        void Send();

        /// <summary>
        /// Sets an object as the Data property of the current execution operation. Use this to track aditional data for the activity
        /// </summary>
        /// <param name="value"></param>
        void SetCustomData(object value);
    }
}

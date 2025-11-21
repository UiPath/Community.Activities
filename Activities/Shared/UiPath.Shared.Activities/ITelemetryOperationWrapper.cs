using System;

namespace UiPath.Shared.Activities
{
    public interface ITelemetryOperationWrapper
    {
        void SendWithException(Exception ex);

        void Send();

        /// <summary>
        /// Sets an object as the Data property of the current execution operation. Use this to track additional data for the activity
        /// </summary>
        /// <param name="value"></param>
        void SetCustomData(object value);

        /// <summary>
        /// Sets a custom "Dictionary{string, string}" key-value pair. <br />
        /// WARNING: it destroys any existing custom data, that is not already a "Dictionary{string, object}"
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        void SetCustomDataKey(string key, object value);
    }
}

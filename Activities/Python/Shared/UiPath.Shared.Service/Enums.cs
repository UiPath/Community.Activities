namespace UiPath.Python.Service
{
    #region Request Type

    internal enum RequestType
    {
        Initialize,
        Shutdown,
        Execute,
        LoadScript,
        InvokeMethod,
        Convert
    }

    #endregion Request Type

    #region Result State

    internal enum ResultState
    {
        Successful,
        InstantiationException,
        LoadException,
        InvocationException,
        ConversionException,
        ExecutionException,
        RequestException
    }

    #endregion Result State
}
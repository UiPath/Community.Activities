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
        InstantiationException,
        IllegalArguments,
        UnknownException,
        ScriptNotLoaded,
        ScriptNotFound,
        ScriptAlreadyLoaded,
        FieldNotFound,
        Successful
    }

    #endregion Result State
}
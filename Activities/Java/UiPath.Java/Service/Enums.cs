
namespace UiPath.Java.Service
{ 
    #region Request Type

    internal enum RequestType
    {
        DoNothing,
        LoadJar,
        StopConnection,
        InvokeConstructor,
        InvokeMethod,
        InvokeStaticMethod,
        GetField
    }

    #endregion

    #region Result State

    internal enum ResultState
    {
        MethodNotFound,
        ConstructorNotFound,
        ClassNotFound,
        InstanceNotFound,
        IllegalAccess,
        InvocationTarget,
        InstantiationException,
        IllegalArguments,
        UnknownException,
        JarNotLoaded,
        JarNotFound,
        JarAlreadyLoaded,
        FieldNotFound,
        Successful,
        RequestNotProcessed
    }

    #endregion
}

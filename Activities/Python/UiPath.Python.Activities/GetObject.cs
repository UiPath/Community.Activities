using System;
using System.Activities;
using System.Diagnostics;
using UiPath.Python.Activities.Properties;
using UiPath.Shared.Activities;
#if ENABLE_DEFAULT_TELEMETRY
using UiPath.Shared.Telemetry.Services;
#endif

namespace UiPath.Python.Activities
{
    /// <summary>
    /// Activity for extracting the .NET object from the Python type
    /// </summary>
    [LocalizedDisplayName(nameof(Resources.GetObjectNameDisplayName))]
    [LocalizedDescription(nameof(Resources.GetObjectDescription))]
    public class GetObject<T> : PythonCodeActivity
    {
        [RequiredArgument]
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.PythonObjectNameDisplayName))]
        [LocalizedDescription(nameof(Resources.PythonObjectDescription))]
        public InArgument<PythonObject> PythonObject { get; set; }

        [LocalizedCategory(nameof(Resources.Output))]
        [LocalizedDisplayName(nameof(Resources.ResultNameDisplayName))]
        [LocalizedDescription(nameof(Resources.GetObjectResultDescription))]
        public OutArgument<T> Result { get; set; }

        protected override void Execute(CodeActivityContext context)
        {
            ITelemetryOperationWrapper telemetryOperation = null;
#if ENABLE_DEFAULT_TELEMETRY
            telemetryOperation = RuntimeTelemetryService.CreateExecutionOperation(this, context);
#endif

            IEngine pythonEngine = PythonScope.GetPythonEngine(context);
            PythonObject pyObject = PythonObject.Get(context);
            if (null == pyObject)
            {
                var ex = new ArgumentNullException(nameof(PythonObject));
                telemetryOperation?.SendWithException(ex);
                throw ex;
            }

            T result;
            try
            {
                result = (T)pythonEngine.Convert(pyObject, typeof(T));
                telemetryOperation?.Send();
            }
            catch (Exception e)
            {
                Trace.TraceError($"Error casting Python object: {e}");
                var ex = new InvalidOperationException(Resources.ConvertException, e);
                telemetryOperation?.SendWithException(ex);
                throw ex;
            }

            Result.Set(context, result);
        }
    }
}
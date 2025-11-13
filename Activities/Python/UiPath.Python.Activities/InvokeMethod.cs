using System;
using System.Activities;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using UiPath.Python.Activities.Properties;
using UiPath.Shared.Activities;

#if ENABLE_DEFAULT_TELEMETRY
using UiPath.Shared.Telemetry.Services;
#endif

namespace UiPath.Python.Activities
{
    /// <summary>
    /// Activity for invoking a Python method
    /// </summary>
    [LocalizedDisplayName(nameof(Resources.InvokeMethodNameDisplayName))]
    [LocalizedDescription(nameof(Resources.InvokeMethodDescription))]
    public class InvokeMethod : PythonActivity
    {
        [DefaultValue(null)]
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.InstanceNameDisplayName))]
        [LocalizedDescription(nameof(Resources.InstanceDescription))]
        public InArgument<PythonObject> Instance { get; set; }

        [RequiredArgument]
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.NameDisplayName))]
        [LocalizedDescription(nameof(Resources.MethodNameDescription))]
        public InArgument<string> Name { get; set; }

        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.ParametersNameDisplayName))]
        [LocalizedDescription(nameof(Resources.ParametersDescription))]
        public InArgument<IEnumerable<object>> Parameters { get; set; }

        [LocalizedCategory(nameof(Resources.Output))]
        [LocalizedDisplayName(nameof(Resources.ResultNameDisplayName))]
        [LocalizedDescription(nameof(Resources.ResultDescription))]
        public OutArgument<PythonObject> Result { get; set; }

        protected async override Task<Action<AsyncCodeActivityContext>> ExecuteAsync(AsyncCodeActivityContext context, CancellationToken cancellationToken)
        {
            ITelemetryOperationWrapper telemetryOperation = null;
#if ENABLE_DEFAULT_TELEMETRY
            telemetryOperation = RuntimeTelemetryService.CreateExecutionOperation(this, context);
#endif

            IEngine pythonEngine = PythonScope.GetPythonEngine(context);
            if (pythonEngine == null)
            {
                var ex = new InvalidOperationException(Resources.PythonEngineNotFoundException);
                telemetryOperation?.SendWithException(ex);
                throw ex;
            }

            PythonObject pyObject = Instance.Get(context);
            string methodName = Name.Get(context);
            IEnumerable<object> parameters = Parameters.Get(context);

            // safeguard checks
            if (methodName.IsNullOrEmpty())
            {
                var ex = new InvalidOperationException(Resources.InvalidMethodNameException);
                telemetryOperation?.SendWithException(ex);
                throw ex;
            }

            PythonObject result = null;
            try
            {
                result = await pythonEngine.InvokeMethod(pyObject, methodName, parameters, cancellationToken);
                telemetryOperation?.Send();
            }
            catch (Exception e)
            {
                Trace.TraceError($"Error invoking Python function: {e}");
                var ex = new InvalidOperationException(Resources.InvokeException, e);
                telemetryOperation?.SendWithException(ex);
                throw ex;
            }

            return asyncCodeActivityContext =>
            {
                Result.Set(asyncCodeActivityContext, result);
            };
        }
    }
}
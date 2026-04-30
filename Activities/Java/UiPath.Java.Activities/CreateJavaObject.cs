using System;
using System.Activities;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UiPath.Java.Activities.Properties;
using UiPath.Shared.Activities;
#if ENABLE_DEFAULT_TELEMETRY
using UiPath.Shared.Telemetry.Services;
#endif


namespace UiPath.Java.Activities
{
    [LocalizedDisplayName(nameof(Resources.CreateJavaObjectDisplayName))]
    [LocalizedDescription(nameof(Resources.CreateJavaObjectDescription))]
    public class CreateJavaObject : JavaActivityWithParameters
    {
        [RequiredArgument]
        [LocalizedDisplayName(nameof(Resources.TargetTypeDisplayName))]
        [LocalizedCategory(nameof(Resources.Target))]
        [LocalizedDescription(nameof(Resources.TargetTypeDescription))]
        public InArgument<string> TargetType { get; set; }

        [LocalizedDisplayName(nameof(Resources.ResultDisplayName))]
        [LocalizedCategory(nameof(Resources.Output))]
        [LocalizedDescription(nameof(Resources.JavaObjectDescription))]
        public OutArgument<JavaObject> Result { get; set; }

        protected async override Task<Action<AsyncCodeActivityContext>> ExecuteAsync(AsyncCodeActivityContext context, CancellationToken cancellationToken)
        {
            ITelemetryOperationWrapper telemetryOperation = null;
#if ENABLE_DEFAULT_TELEMETRY
            telemetryOperation = RuntimeTelemetryService.CreateExecutionOperation(this, context);
#endif
            try
            {
                IInvoker invoker = JavaScope.GetJavaInvoker(context);
                var className = TargetType.Get(context);
                if (string.IsNullOrWhiteSpace(className))
                    throw new ArgumentNullException(nameof(TargetType));

                List<object> parameters = GetParameters(context);
                var types = GetParameterTypes(context, parameters);
                JavaObject instance = null;
                try
                {
                    instance = await invoker.InvokeConstructor(className, parameters, types, cancellationToken);
                }
                catch (Exception e)
                {
                    Trace.TraceError($"Constructor could not be invoked: {e}");
                    throw new InvalidOperationException(Resources.ConstructorException, e);
                }

                var result = new Action<AsyncCodeActivityContext>(asyncCodeActivityContext =>
                {
                    Result.Set(asyncCodeActivityContext, instance);
                });

                telemetryOperation?.Send();
                return result;
            }
            catch (Exception ex)
            {
                telemetryOperation?.SendWithException(ex);
                throw;
            }
        }
    }
}

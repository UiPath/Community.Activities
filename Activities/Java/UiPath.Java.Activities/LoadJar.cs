using System;
using System.Activities;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using UiPath.Java.Activities.Properties;
using UiPath.Shared.Activities;
#if ENABLE_DEFAULT_TELEMETRY
using UiPath.Shared.Telemetry.Services;
#endif


namespace UiPath.Java.Activities
{
    [LocalizedDisplayName(nameof(Resources.LoadJarDisplayName))]
    [LocalizedDescription(nameof(Resources.LoadJarDescription))]
    public class LoadJar : JavaActivity
    {
        [LocalizedDisplayName(nameof(Resources.JarPathDisplayName))]
        [LocalizedDescription(nameof(Resources.JarPathDescription))]
        [LocalizedCategory(nameof(Resources.Input))]
        [RequiredArgument]
        public InArgument<string> JarPath { get; set; }

        protected async override Task<Action<AsyncCodeActivityContext>> ExecuteAsync(AsyncCodeActivityContext context, CancellationToken cancellationToken)
        {
            ITelemetryOperationWrapper telemetryOperation = null;
#if ENABLE_DEFAULT_TELEMETRY
            telemetryOperation = RuntimeTelemetryService.CreateExecutionOperation(this, context);
#endif
            try
            {
                IInvoker invoker = JavaScope.GetJavaInvoker(context);
                var jarPath = JarPath.Get(context) ?? throw new ArgumentNullException(Resources.JarPathDisplayName);
                try
                {
                    await invoker.LoadJar(jarPath, cancellationToken);
                }
                catch (Exception e)
                {
                    Trace.TraceError($"Jar could not be loaded{e}");
                    throw new InvalidOperationException(Resources.LoadJarException, e);
                }
                var result = new Action<AsyncCodeActivityContext>(asyncCodeActivityContext =>
                {
                    // No OutArgument
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

using System;
using System.Activities;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UiPath.Java.Activities.Properties;

namespace UiPath.Java.Activities
{
    [LocalizedDisplayName(nameof(Resources.InvokeJavaMethodDisplayName))]
    [LocalizedDescription(nameof(Resources.InvokeJavaMethodDescription))]
    public class InvokeJavaMethod : JavaActivityWithParameters
    {
        [RequiredArgument]
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.MethodNameDisplayName))]
        [LocalizedDescription(nameof(Resources.MethodNameDescription))]
        public InArgument<string> MethodName { get; set; }

        [OverloadGroup("Instance")]
        [RequiredArgument]
        [LocalizedCategory(nameof(Resources.Target))]
        [LocalizedDisplayName(nameof(Resources.TargetObjectDisplayName))]
        [LocalizedDescription(nameof(Resources.TargetObjectDescription))]
        public InArgument<JavaObject> TargetObject { get; set; }

        [OverloadGroup("Static")]
        [RequiredArgument]
        [LocalizedCategory(nameof(Resources.Target))]
        [LocalizedDisplayName(nameof(Resources.TargetTypeDisplayName))]
        [LocalizedDescription(nameof(Resources.TargetTypeDescription))]
        public InArgument<string> TargetType { get; set; }

        [LocalizedCategory(nameof(Resources.Output))]
        [LocalizedDisplayName(nameof(Resources.ResultDisplayName))]
        [LocalizedDescription(nameof(Resources.JavaObjectDescription))]
        public OutArgument<JavaObject> Result { get; set; }

        protected async override Task<Action<AsyncCodeActivityContext>> ExecuteAsync(AsyncCodeActivityContext context, CancellationToken cancellationToken)
        {
            IInvoker invoker = JavaScope.GetJavaInvoker(context);
            var methodName = MethodName.Get(context) ?? throw new ArgumentNullException(Resources.MethodName);
            JavaObject javaObject = TargetObject.Get(context);
            string className = TargetType.Get(context);

            if (javaObject == null && string.IsNullOrWhiteSpace(className))
            {
                throw new InvalidOperationException(Resources.InvokationObjectException);
            }

            List<object> parameters = GetParameters(context);
            var types = GetParameterTypes(context);
            JavaObject instance = null;

            try
            {
                instance = await invoker.InvokeMethod(methodName, className, javaObject, parameters, types, cancellationToken);
            }
            catch (Exception e)
            {
                Trace.TraceError($"The method could not be invoked: {e.ToString()}");
                throw new InvalidOperationException(Resources.InvokeMethodException, e);
            }

            return asyncCodeActivityContext =>
            {
                Result.Set(asyncCodeActivityContext, instance);
            };
        }
    }
}

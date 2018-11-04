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
            IInvoker invoker = JavaScope.GetJavaInvoker(context);
            var className = TargetType.Get(context);
            if (string.IsNullOrWhiteSpace(className))
            {
                throw new ArgumentNullException(nameof(TargetType));
            }
            List<object> parameters = GetParameters(context);

            JavaObject instance = null;
            try
            {
                instance = await invoker.InvokeConstructor(className, parameters, parameters.Select(param => param?.GetType()).ToList(), cancellationToken);
            }
            catch (Exception e)
            {
                Trace.TraceError($"Constrcutor could not be invoker: {e.ToString()}");
                throw new InvalidOperationException(Resources.ConstructorException, e);
            }

            return asyncCodeActivityContext =>
            {
                Result.Set(asyncCodeActivityContext, instance);
            };
        }
    }
}

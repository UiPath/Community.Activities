using System;
using System.Activities;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using UiPath.Java.Activities.Properties;

namespace UiPath.Java.Activities
{
    [LocalizedDisplayName(nameof(Resources.GetFieldDisplayName))]
    [LocalizedDescription(nameof(Resources.GetFieldDescritption))]
    public class GetJavaField : JavaActivity
    {
        [RequiredArgument]
        [LocalizedDisplayName(nameof(Resources.FieldNameDisplayName))]
        [LocalizedDescription(nameof(Resources.FieldNameDescription))]
        [LocalizedCategory(nameof(Resources.Input))]
        public InArgument<string> FieldName { get; set; }

        [OverloadGroup("Instance")]
        [RequiredArgument]
        [LocalizedDisplayName(nameof(Resources.TargetObjectDisplayName))]
        [LocalizedDescription(nameof(Resources.TargetObjectDescription))]
        [LocalizedCategory(nameof(Resources.Target))]
        public InArgument<JavaObject> TargetObject { get; set; }

        [OverloadGroup("Static")]
        [RequiredArgument]
        [LocalizedDisplayName(nameof(Resources.TargetTypeDisplayName))]
        [LocalizedDescription(nameof(Resources.TargetTypeDescription))]
        [LocalizedCategory(nameof(Resources.Target))]
        public InArgument<string> TargetType { get; set; }

        [LocalizedDisplayName(nameof(Resources.ResultDisplayName))]
        [LocalizedCategory(nameof(Resources.Output))]
        [LocalizedDescription(nameof(Resources.JavaObjectDescription))]
        public OutArgument<JavaObject> Result { get; set; }
        protected async override Task<Action<AsyncCodeActivityContext>> ExecuteAsync(AsyncCodeActivityContext context, CancellationToken cancellationToken)
        {
            IInvoker invoker = JavaScope.GetJavaInvoker(context);
            var fieldName = FieldName.Get(context) ?? throw new ArgumentNullException(Resources.FieldName);
            var javaObject = TargetObject.Get(context);
            var className = TargetType.Get(context);

            if (javaObject == null && className == null)
            {
                throw new InvalidOperationException(Resources.InvokationObjectException);
            }

            JavaObject instance;
            try
            {
                instance = await invoker.InvokeGetField(javaObject, fieldName, className, cancellationToken);
            }
            catch (Exception e)
            {
                Trace.TraceError($"Could not get java field: {e.ToString()}");
                throw new InvalidOperationException(Resources.GetFieldException, e);
            }

            return asyncCodeActivityContext =>
            {
                Result.Set(asyncCodeActivityContext, instance);
            };
        }
    }
}

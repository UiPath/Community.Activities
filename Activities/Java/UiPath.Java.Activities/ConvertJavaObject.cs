using System;
using System.Activities;
using System.ComponentModel;
using UiPath.Java.Activities.Properties;

namespace UiPath.Java.Activities
{
    [LocalizedDisplayName(nameof(Resources.ConvertJavaObjectDisplayName))]
    [LocalizedDescription(nameof(Resources.ConvertJavaObjectDescription))]
    public class ConvertJavaObject<T> : JavaCodeActivity
    {
        [RequiredArgument]
        [LocalizedDisplayName(nameof(Resources.JavaObjectDisplayName))]
        [LocalizedDescription(nameof(Resources.JavaObjectDescription))]
        [LocalizedCategory(nameof(Resources.Input))]
        public InArgument<JavaObject> JavaObject { get; set; }

        [LocalizedDisplayName(nameof(Resources.ResultDisplayName))]
        [LocalizedDescription(nameof(Resources.ConvertJavaObjectResultDescription))]
        [LocalizedCategory(nameof(Resources.Output))]
        public OutArgument<T> Result { get; set; }

        [Browsable(false)]
        public Type ActivityType
        {
            get
            {
                return typeof(T);
            }

            private set
            {

            }
        }

        protected override void Execute(CodeActivityContext context)
        {
            IInvoker invoker = JavaScope.GetJavaInvoker(context);
            var javaObject = JavaObject.Get(context) ?? throw new ArgumentNullException(Resources.JavaObject);
            Result.Set(context, javaObject.Convert<T>());
        }
    }
}

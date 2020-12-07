using System;
using System.Activities;
using System.Activities.Statements;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UiPath.Java.Activities.Properties;
using UiPath.Shared.Activities;

namespace UiPath.Java.Activities
{
    [LocalizedDisplayName(nameof(Resources.JavaScopeNameDisplayName))]
    [LocalizedDescription(nameof(Resources.JavaScopeDescription))]
    public class JavaScope : AsyncTaskNativeActivity
    {
        [LocalizedDisplayName(nameof(Resources.JavaPathDisplayName))]
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDescription(nameof(Resources.JavaPathDescription))]
        public InArgument<string> JavaPath { get; set; }

        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.TimeoutMSDisplayName))]
        [LocalizedDescription(nameof(Resources.TimeoutMSDescription))]
        [DefaultValue(15000)]
        public InArgument<int> TimeoutMS { get; set; }

        [Browsable(false)]
        public ActivityAction<object> Body { get; set; }

        private IInvoker _invoker;

        internal static IInvoker GetJavaInvoker(ActivityContext context)
        {
            IInvoker invoker = context.DataContext.GetProperties()[JavaInvokerProperty]?.GetValue(context.DataContext) as IInvoker;
            if (invoker == null)
            {
                throw new InvalidOperationException(Resources.JavaInvokerNotLoadedException);
            }
            return invoker;
        }

        private const string JavaInvokerProperty = "JavaInvokerProperty";

        public JavaScope()
        {
            Body = new ActivityAction<object>
            {
                Argument = new DelegateInArgument<object>(JavaInvokerProperty),
                Handler = new Sequence()
                {
                    DisplayName = Resources.Do
                }
            };
        }

        protected override async Task<Action<NativeActivityContext>> ExecuteAsync(NativeActivityContext context, CancellationToken ct)
        {
            string javaPath = JavaPath.Get(context);
            if (javaPath != null)
            {
                javaPath = Path.Combine(javaPath, "bin", "java.exe");
                if (!File.Exists(javaPath))
                {
                    throw new ArgumentException(Resources.InvalidJavaPath, Resources.JavaPathDisplayName);
                }
            }
            _invoker = new JavaInvoker(javaPath);

            int initTimeout = TimeoutMS.Get(context);
            if (initTimeout < 0)
            {
                throw new ArgumentException(UiPath.Java.Activities.Properties.Resources.TimeoutMSException, "TimeoutMS");
            }

            try
            {
                await _invoker.StartJavaService(initTimeout);
            }
            catch (Exception e)
            {
                Trace.TraceError($"Error initializing Java Invoker: {e.ToString()}");
                throw new InvalidOperationException(string.Format(Resources.JavaInitiazeException, e.ToString()));
            }
            ct.ThrowIfCancellationRequested();
            return ctx =>
            {
                ctx.ScheduleAction(Body, _invoker, OnCompleted, OnFaulted);
            };
        }

        private void OnFaulted(NativeActivityFaultContext faultContext, Exception propagatedException, ActivityInstance propagatedFrom)
        {
            faultContext.CancelChildren();
            Clean().DoNotAwait();
        }

        private void OnCompleted(NativeActivityContext context, ActivityInstance completedInstance)
        {
            Clean().DoNotAwait();
        }

        protected override void Cancel(NativeActivityContext context)
        {
            Clean().DoNotAwait();
            base.Cancel(context);
        }

        protected override void Abort(NativeActivityAbortContext context)
        {
            Clean().DoNotAwait();
            base.Abort(context);
        }

        public async Task Clean()
        {
            await _invoker?.ReleaseAsync();
            _invoker = null;
        }
    }
}

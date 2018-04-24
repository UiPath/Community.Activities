using System;
using System.Activities;
using System.Threading;
using System.Threading.Tasks;

namespace UiPath.Shared.Activities.Tests
{
    internal class FailsAfterDelayNativeActivity : AsyncTaskNativeActivityContinue
    {
        public InArgument<int> Delay { get; set; }

        protected override async Task<Action<NativeActivityContext>> ExecuteAsync(NativeActivityContext context, CancellationToken cancellationToken)
        {
            int delay = Delay.Get(context);

            Task taskThatFails = Task.Run(async () =>
            {
                await Task.Delay(delay, cancellationToken).ContinueWith(t => throw new NotImplementedException(), cancellationToken);
            }, cancellationToken);

            await taskThatFails;

            return (nativeActivityContext) =>
            {

            };
        }
    }

    internal class FailsWhenReturningNativeActivity : AsyncTaskNativeActivityContinue
    {
        protected override async Task<Action<NativeActivityContext>> ExecuteAsync(NativeActivityContext context, CancellationToken cancellationToken)
        {
            Task taskThatRuns = Task.Run(async () =>
            {
                await Task.Delay(100, cancellationToken);
            }, cancellationToken);

            await taskThatRuns;

            return (asyncCodeActivityContext) => throw new NotImplementedException();
        }
    }

    internal class FailsAfterDelayCodeActivity : AsyncTaskCodeActivityContinue
    {
        public InArgument<int> Delay { get; set; }

        protected override async Task<Action<AsyncCodeActivityContext>> ExecuteAsync(AsyncCodeActivityContext context, CancellationToken cancellationToken)
        {
            int delay = Delay.Get(context);

            Task taskThatFails = Task.Run(async () =>
            {
                await Task.Delay(delay, cancellationToken).ContinueWith(t => throw new NotImplementedException(), cancellationToken);
            }, cancellationToken);

            await taskThatFails;

            return (asyncCodeActivityContext) =>
            {

            };
        }
    }

    internal class FailsWhenReturningCodeActivity : AsyncTaskCodeActivityContinue
    {
        protected override async Task<Action<AsyncCodeActivityContext>> ExecuteAsync(AsyncCodeActivityContext context, CancellationToken cancellationToken)
        {
            Task taskThatRuns = Task.Run(async () =>
            {
                await Task.Delay(100, cancellationToken);
            }, cancellationToken);

            await taskThatRuns;

            return (asyncCodeActivityContext) => throw new NotImplementedException();
        }
    }
}

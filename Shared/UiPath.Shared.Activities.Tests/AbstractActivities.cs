using Microsoft.Activities.UnitTesting;
using NUnit.Framework;
using System;

namespace UiPath.Shared.Activities.Tests
{
    [TestFixture]
    public class AbstractActivities
    {
        [TestCase]
        public void NativeActivity_ContinuesWhenInnerTaskFails()
        {
            int delay = 1000;

            FailsAfterDelayNativeActivity mock = new FailsAfterDelayNativeActivity()
            {
                Delay = delay,
                ContinueOnError = true
            };
            WorkflowInvokerTest invoker = new WorkflowInvokerTest(mock);

            invoker.TestActivity(TimeSpan.FromMilliseconds(delay * 2));
        }

        [TestCase]
        public void NativeActivity_FailsWhenInnerTaskFails()
        {
            int delay = 1000;

            FailsAfterDelayNativeActivity mock = new FailsAfterDelayNativeActivity()
            {
                Delay = delay,
                ContinueOnError = false
            };
            WorkflowInvokerTest invoker = new WorkflowInvokerTest(mock);

            Assert.Throws<NotImplementedException>(() => { invoker.TestActivity(TimeSpan.FromMilliseconds(delay * 2)); });
        }

        [TestCase]
        public void NativeActivity_ContinuesWhenReturnedDelegateFails()
        {
            FailsWhenReturningNativeActivity mock = new FailsWhenReturningNativeActivity()
            {
                ContinueOnError = true
            };
            WorkflowInvokerTest invoker = new WorkflowInvokerTest(mock);

            invoker.TestActivity();
        }

        [TestCase]
        public void NativeActivity_FailsWhenReturnedDelegateFails()
        {
            FailsWhenReturningNativeActivity mock = new FailsWhenReturningNativeActivity()
            {
                ContinueOnError = false
            };
            WorkflowInvokerTest invoker = new WorkflowInvokerTest(mock);

            Assert.Throws<NotImplementedException>(() => { invoker.TestActivity(); });
        }

        [TestCase]
        public void CodeActivity_ContinuesWhenInnerTaskFails()
        {
            int delay = 1000;

            FailsAfterDelayCodeActivity mock = new FailsAfterDelayCodeActivity()
            {
                Delay = delay,
                ContinueOnError = true
            };
            WorkflowInvokerTest invoker = new WorkflowInvokerTest(mock);

            invoker.TestActivity(TimeSpan.FromMilliseconds(delay * 2));
        }

        [TestCase]
        public void CodeActivity_FailsWhenInnerTaskFails()
        {
            int delay = 1000;

            FailsAfterDelayCodeActivity mock = new FailsAfterDelayCodeActivity()
            {
                Delay = delay,
                ContinueOnError = false
            };
            WorkflowInvokerTest invoker = new WorkflowInvokerTest(mock);

            Assert.Throws<NotImplementedException>(() => { invoker.TestActivity(TimeSpan.FromMilliseconds(delay * 2)); });
        }

        [TestCase]
        public void CodeActivity_ContinuesWhenReturnedDelegateFails()
        {
            FailsWhenReturningCodeActivity mock = new FailsWhenReturningCodeActivity()
            {
                ContinueOnError = true
            };
            WorkflowInvokerTest invoker = new WorkflowInvokerTest(mock);

            invoker.TestActivity();
        }

        [TestCase]
        public void CodeActivity_FailsWhenReturnedDelegateFails()
        {
            FailsWhenReturningCodeActivity mock = new FailsWhenReturningCodeActivity()
            {
                ContinueOnError = false
            };
            WorkflowInvokerTest invoker = new WorkflowInvokerTest(mock);

            Assert.Throws<NotImplementedException>(() => { invoker.TestActivity(); });
        }
    }
}

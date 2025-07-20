using Xunit;
using System;
using System.Activities;

namespace UiPath.Shared.Activities.Tests
{
    
    public class AbstractActivities
    {
        [Fact]
        public void NativeActivity_ContinuesWhenInnerTaskFails()
        {
            int delay = 1000;

            FailsAfterDelayNativeActivity mock = new FailsAfterDelayNativeActivity()
            {
                Delay = delay,
                ContinueOnError = true
            };
            
            WorkflowInvoker.Invoke(mock, TimeSpan.FromMilliseconds(delay * 2));
        }

        [Fact]
        public void NativeActivity_FailsWhenInnerTaskFails()
        {
            int delay = 1000;

            FailsAfterDelayNativeActivity mock = new FailsAfterDelayNativeActivity()
            {
                Delay = delay,
                ContinueOnError = false
            };

            Assert.Throws<NotImplementedException>(() => { WorkflowInvoker.Invoke(mock, TimeSpan.FromMilliseconds(delay * 2)); });
        }

        [Fact]
        public void NativeActivity_ContinuesWhenReturnedDelegateFails()
        {
            FailsWhenReturningNativeActivity mock = new FailsWhenReturningNativeActivity()
            {
                ContinueOnError = true
            };

            WorkflowInvoker.Invoke(mock, TimeSpan.FromSeconds(30));
        }

        [Fact]
        public void NativeActivity_FailsWhenReturnedDelegateFails()
        {
            FailsWhenReturningNativeActivity mock = new FailsWhenReturningNativeActivity()
            {
                ContinueOnError = false
            };

            Assert.Throws<NotImplementedException>(() => { WorkflowInvoker.Invoke(mock, TimeSpan.FromSeconds(30)); });
        }

        [Fact]
        public void CodeActivity_ContinuesWhenInnerTaskFails()
        {
            int delay = 1000;

            FailsAfterDelayCodeActivity mock = new FailsAfterDelayCodeActivity()
            {
                Delay = delay,
                ContinueOnError = true
            };

            WorkflowInvoker.Invoke(mock, TimeSpan.FromMilliseconds(delay * 2));
        }

        [Fact]
        public void CodeActivity_FailsWhenInnerTaskFails()
        {
            int delay = 1000;

            FailsAfterDelayCodeActivity mock = new FailsAfterDelayCodeActivity()
            {
                Delay = delay,
                ContinueOnError = false
            };

            Assert.Throws<NotImplementedException>(() => { WorkflowInvoker.Invoke(mock, TimeSpan.FromMilliseconds(delay * 2)); });
        }

        [Fact]
        public void CodeActivity_ContinuesWhenReturnedDelegateFails()
        {
            FailsWhenReturningCodeActivity mock = new FailsWhenReturningCodeActivity()
            {
                ContinueOnError = true
            };

            WorkflowInvoker.Invoke(mock, TimeSpan.FromSeconds(30));
        }

        [Fact]
        public void CodeActivity_FailsWhenReturnedDelegateFails()
        {
            FailsWhenReturningCodeActivity mock = new FailsWhenReturningCodeActivity()
            {
                ContinueOnError = false
            };

            Assert.Throws<NotImplementedException>(() => { WorkflowInvoker.Invoke(mock, TimeSpan.FromSeconds(30)); });
        }
    }
}

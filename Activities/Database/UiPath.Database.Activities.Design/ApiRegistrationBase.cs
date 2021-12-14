using System;
using System.Diagnostics;
using UiPath.Studio.Activities.Api;

namespace UiPath.Database.Activities.Design
{
    internal abstract class ApiRegistrationBase
    {
        public void Initialize(IWorkflowDesignApi api)
        {
            try
            {
                if (!CanPerformRegistration(api))
                    return;
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.ToString());
            }

            // Separate method to prevent JIT compilation exception
            // in case the api is not supported (for older Studio)
            PerformRegistration(api);
        }

        public abstract bool CanPerformRegistration(IWorkflowDesignApi api);

        protected abstract void PerformRegistration(IWorkflowDesignApi api);
    }
}

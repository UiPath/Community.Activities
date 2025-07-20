using System;
using System.Diagnostics;
using UiPath.Studio.Activities.Api;

namespace UiPath.FTP.Activities.Design
{
    /// <summary>
    /// Base class for API registration, taken from Activities Repo
    /// </summary>
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

        /// <summary>
        /// Checks whether the registration can be performed
        /// </summary>
        /// <param name="api">The api</param>
        /// <returns>True or false, meaning whether the registration can be performed or not</returns>
        public abstract bool CanPerformRegistration(IWorkflowDesignApi api);

        /// <summary>
        /// Performs the registration
        /// </summary>
        /// <param name="api">The api</param>
        protected abstract void PerformRegistration(IWorkflowDesignApi api);
    }
}

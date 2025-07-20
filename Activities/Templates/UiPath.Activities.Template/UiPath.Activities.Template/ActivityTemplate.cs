using System.Activities;
using System.Diagnostics;

namespace $safeprojectname$
{
    public class ActivityTemplate : CodeActivity // This base class exposes an OutArgument named Result
    {
        /*
         * The returned value will be used to set the value of the Result argument
         */
        protected override void Execute(CodeActivityContext context)
        {
            ExecuteInternal();
        }

        public void ExecuteInternal()
        {
            // use this to automatically attach the debugger to the process
            //Debugger.Launch();
            throw new NotImplementedException();
        }
    }
}

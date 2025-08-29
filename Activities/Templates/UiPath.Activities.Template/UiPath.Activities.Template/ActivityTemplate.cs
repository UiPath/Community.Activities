using System.Activities;
using System.Diagnostics;

namespace $safeprojectname$
{
    public class ActivityTemplate : CodeActivity<int> // This base class exposes an OutArgument named Result
    {
        /*
         * The returned value will be used to set the value of the Result argument
         */
        protected override int Execute(CodeActivityContext context)
        {
            return ExecuteInternal();
        }

        public int ExecuteInternal()
        {
            // use this to automatically attach the debugger to the process
            //Debugger.Launch();
            throw new NotImplementedException();
        }
    }
}

using System.Activities;

namespace UiPath.Activities.Template
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
            throw new NotImplementedException();
        }
    }
}

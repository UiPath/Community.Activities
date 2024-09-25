using System.Activities;
using UiPath.Python.Activities.Properties;
using UiPath.Shared.Activities;

namespace UiPath.Python.Activities
{
    public abstract class PythonActivity : UiPath.Shared.Activities.AsyncTaskCodeActivity
    {
        protected PythonActivity()
        {
            Constraints.Add(ActivityConstraints.HasParentType<PythonActivity, PythonScope>(string.Format(Resources.ValidateParentError, typeof(PythonScope).Name)));
        }
    }

    public abstract class PythonCodeActivity : CodeActivity
    {
        protected PythonCodeActivity()
        {
            Constraints.Add(ActivityConstraints.HasParentType<PythonCodeActivity, PythonScope>(string.Format(Resources.ValidateParentError, typeof(PythonScope).Name)));
        }
    }
}

using System;
using System.Activities;
using System.Collections.Generic;
using UiPath.Java.Activities.Properties;
using UiPath.Shared.Activities;

namespace UiPath.Java.Activities
{
    public abstract class JavaActivity : AsyncTaskCodeActivity
    {
        protected JavaActivity()
        {
            Constraints.Add(ActivityConstraints.HasParentType<JavaActivity, JavaScope>(string.Format(Resources.ValidateParentError, typeof(JavaScope).Name)));
        }
    }

    public abstract class JavaCodeActivity : CodeActivity
    {
        protected JavaCodeActivity()
        {
            Constraints.Add(ActivityConstraints.HasParentType<JavaCodeActivity, JavaScope>(string.Format(Resources.ValidateParentError, typeof(JavaScope).Name)));
        }
    }
}

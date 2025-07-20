using System;
using System.Activities;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UiPath.FTP.Activities.Properties;
using UiPath.Shared.Activities;

namespace UiPath.FTP.Activities
{
    public abstract class FtpAsyncActivity : ContinuableAsyncCodeActivity
    {
        protected FtpAsyncActivity()
        {
            Constraints.Add(ActivityConstraints.HasParentType<FtpAsyncActivity, WithFtpSession>(string.Format(Resources.ValidationError_ValidateParentError,
                Resources.Activity_WithFtpSession_Property_DisplayName_Name)));
        }
    }
    public abstract class FtpCodeActivity : CodeActivity
    {
        protected FtpCodeActivity()
        {
            Constraints.Add(ActivityConstraints.HasParentType<FtpCodeActivity, WithFtpSession>(string.Format(Resources.ValidationError_ValidateParentError,
                Resources.Activity_WithFtpSession_Property_DisplayName_Name)));
        }
    }
}

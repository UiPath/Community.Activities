using CredentialManagement;
using System;
using System.Activities;
using UiPath.Credentials.Activities.Properties;
using UiPath.Shared.Activities;

#if ENABLE_DEFAULT_TELEMETRY
using UiPath.Shared.Telemetry.Services;
#endif

namespace UiPath.Credentials.Activities
{
    public class DeleteCredential : CodeActivity<bool>
    {
        [RequiredArgument]
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.TargetDisplayName))]
        [LocalizedDescription(nameof(Resources.TargetDescription))]
        public InArgument<string> Target { get; set; }

        protected override bool Execute(CodeActivityContext context)
        {
            ITelemetryOperationWrapper telemetryOperation = null;
#if ENABLE_DEFAULT_TELEMETRY
            telemetryOperation = RuntimeTelemetryService.CreateExecutionOperation(this, context);
#endif

            try
            {
                Credential credential = new Credential { Target = Target.Get(context) };

                telemetryOperation?.Send();

                return credential.Delete();
            }
            catch (Exception ex)
            {
                telemetryOperation?.SendWithException(ex);
                throw;
            }
        }
    }
}

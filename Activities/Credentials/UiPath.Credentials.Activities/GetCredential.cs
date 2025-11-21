using CredentialManagement;
using System;
using System.Activities;
using System.ComponentModel;
using UiPath.Credentials.Activities.Properties;
using UiPath.Shared.Activities;

#if ENABLE_DEFAULT_TELEMETRY
using UiPath.Shared.Telemetry.Services;
#endif

namespace UiPath.Credentials.Activities
{
    [Browsable(false)]
    public class GetCredential : CodeActivity<bool>
    {
        [RequiredArgument]
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.TargetDisplayName))]
        [LocalizedDescription(nameof(Resources.TargetDescription))]
        public InArgument<string> Target { get; set; }

        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_GetCredential_Property_CredentialType_Name))]
        [LocalizedDescription(nameof(Resources.Activity_GetCredential_Property_CredentialType_Description))]
        public CredentialType CredentialType { get; set; }

        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.PersistanceTypeDisplayName))]
        [LocalizedDescription(nameof(Resources.PersistanceTypeDescription))]
        public PersistanceType PersistanceType { get; set; }

        [LocalizedCategory(nameof(Resources.Output))]
        [LocalizedDisplayName(nameof(Resources.UsernameDisplayName))]
        [LocalizedDescription(nameof(Resources.UsernameDescription))]
        public OutArgument<string> Username { get; set; }

        [LocalizedCategory(nameof(Resources.Output))]
        [LocalizedDisplayName(nameof(Resources.PasswordDisplayName))]
        [LocalizedDescription(nameof(Resources.PasswordDescription))]
        public OutArgument<string> Password { get; set; }

        public GetCredential()
        {
            CredentialType = CredentialType.Generic;
            PersistanceType = PersistanceType.Enterprise;
        }

        protected override bool Execute(CodeActivityContext context)
        {
            ITelemetryOperationWrapper telemetryOperation = null;
#if ENABLE_DEFAULT_TELEMETRY
            telemetryOperation = RuntimeTelemetryService.CreateExecutionOperation(this, context);
#endif
            try
            {
                telemetryOperation?.SetCustomDataKey(nameof(CredentialType), CredentialType.ToString() ?? null);
                telemetryOperation?.SetCustomDataKey(nameof(PersistanceType), PersistanceType.ToString() ?? null);

                Credential credential = new Credential { Target = Target.Get(context), Type = CredentialType, PersistanceType = PersistanceType };
                var result = credential.Load();
                if (!result) return false;
                Username.Set(context, credential.Username);
                Password.Set(context, credential.Password);

                telemetryOperation?.Send();

                return true;
            }
            catch (Exception ex)
            {
                telemetryOperation?.SendWithException(ex);
                throw;
            }
        }
    }
}

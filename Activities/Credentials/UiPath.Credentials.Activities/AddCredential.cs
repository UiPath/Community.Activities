using CredentialManagement;
using System.Activities;
using System;
using System.Activities.Validation;
using UiPath.Credentials.Activities.Properties;
using System.Security;
using System.Net;
using UiPath.Shared.Activities;

#if ENABLE_DEFAULT_TELEMETRY
using UiPath.Shared.Telemetry.Services;
#endif

namespace UiPath.Credentials.Activities
{
    public class AddCredential : CodeActivity<bool>
    {
        [RequiredArgument]
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.UsernameDisplayName))]
        [LocalizedDescription(nameof(Resources.UsernameDescription))]
        public InArgument<string> Username { get; set; }

        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.PasswordDisplayName))]
        [LocalizedDescription(nameof(Resources.PasswordDescription))]
        public InArgument<string> Password { get; set; }

        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.PasswordSecureStringDisplayName))]
        [LocalizedDescription(nameof(Resources.PasswordSecureStringDescription))]
        public InArgument<SecureString> PasswordSecureString { get; set; }

        [RequiredArgument]
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.TargetDisplayName))]
        [LocalizedDescription(nameof(Resources.TargetDescription))]
        public InArgument<string> Target { get; set; }

        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_AddCredential_Property_CredentialType_Name))]
        [LocalizedDescription(nameof(Resources.Activity_AddCredential_Property_CredentialType_Description))]
        public CredentialType CredentialType { get; set; }

        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.PersistanceTypeDisplayName))]
        [LocalizedDescription(nameof(Resources.PersistanceTypeDescription))]
        public PersistanceType PersistanceType { get; set; }

        public AddCredential()
        {
            CredentialType = CredentialType.Generic;
            PersistanceType = PersistanceType.Enterprise;
        }
        protected override void CacheMetadata(CodeActivityMetadata metadata)
        {
            base.CacheMetadata(metadata);

            if (!(CredentialType == CredentialType.Generic || CredentialType == CredentialType.DomainPassword))
                metadata.AddValidationError(new ValidationError(Resources.ValidationError_InvalidCredentialType,false,nameof(CredentialType)));
        }

        protected override bool Execute(CodeActivityContext context)
        {
            ITelemetryOperationWrapper telemetryOperation = null;
#if ENABLE_DEFAULT_TELEMETRY
            telemetryOperation = RuntimeTelemetryService.CreateExecutionOperation(this, context);
#endif

            SecureString passwordSecureString = PasswordSecureString.Get(context);
            string password = Password.Get(context);

            telemetryOperation?.SetCustomDataKey(nameof(Password) + CredentialsConstants.IsUsed, !string.IsNullOrWhiteSpace(password));
            telemetryOperation?.SetCustomDataKey(nameof(PasswordSecureString) + CredentialsConstants.IsUsed, passwordSecureString != null);
            telemetryOperation?.SetCustomDataKey(nameof(CredentialType), CredentialType.ToString() ?? null);
            telemetryOperation?.SetCustomDataKey(nameof(PersistanceType), PersistanceType.ToString() ?? null);

            try
            {
                if (string.IsNullOrWhiteSpace(password) && passwordSecureString == null)
                {
                    throw new ArgumentNullException(Resources.PasswordAndSecureStringNull);
                }
                if (password != null && passwordSecureString != null)
                {
                    throw new ArgumentException(Resources.PasswordAndSecureStringNotNull);
                }

                Credential credential = new Credential { Target = Target.Get(context), Username = Username.Get(context), Password = password != null ? password : new NetworkCredential("", passwordSecureString).Password, Type = CredentialType, PersistanceType = PersistanceType };

                telemetryOperation?.Send();

                return credential.Save();
            }
            catch (Exception ex)
            {
                telemetryOperation?.SendWithException(ex);
                throw;
            }
        }
    }
}

using CredentialManagement;
using System.Activities;
using System.ComponentModel;
using System.Security;
using UiPath.Credentials.Activities.Properties;

namespace UiPath.Credentials.Activities
{
    [LocalizedDescription(nameof(Resources.Category))]
    public class GetSecureCredential : CodeActivity<bool>
    {
        [RequiredArgument]
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.TargetDisplayName))]
        [LocalizedDescription(nameof(Resources.TargetDescription))]
        public InArgument<string> Target { get; set; }

        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.CredentialTypeDisplayName))]
        [LocalizedDescription(nameof(Resources.CredentialTypeDescription))]
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
        public OutArgument<SecureString> Password { get; set; }

        public GetSecureCredential()
        {
            CredentialType = CredentialType.Generic;
            PersistanceType = PersistanceType.Enterprise;
        }

        protected override bool Execute(CodeActivityContext context)
        {
            Credential credential = new Credential { Target = Target.Get(context), Type = CredentialType, PersistanceType = PersistanceType };
            var result = credential.Load();
            if (!result) return false;
            Username.Set(context, credential.Username);
            Password.Set(context, credential.SecurePassword);
            return true;
        }
    }
}
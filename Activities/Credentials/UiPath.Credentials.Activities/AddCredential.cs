using CredentialManagement;
using System.Activities;
using System.ComponentModel;
using System;
using System.Activities.Validation;
using System.Diagnostics;
using System.IO;
using UiPath.Credentials.Activities.Properties;

namespace UiPath.Credentials.Activities
{
    public class AddCredential : CodeActivity<bool>
    {
        [RequiredArgument]
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.UsernameDisplayName))]
        [LocalizedDescription(nameof(Resources.UsernameDescription))]
        public InArgument<string> Username { get; set; }

        [RequiredArgument]
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.PasswordDisplayName))]
        [LocalizedDescription(nameof(Resources.PasswordDescription))]
        public InArgument<string> Password { get; set; }

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

        public AddCredential()
        {
            CredentialType = CredentialType.Generic;
            PersistanceType = PersistanceType.Enterprise;
        }

        protected override bool Execute(CodeActivityContext context)
        {
            Credential credential = new Credential { Target = Target.Get(context), Username = Username.Get(context), Password = Password.Get(context), Type = CredentialType, PersistanceType = PersistanceType };
            return credential.Save();
        }
    }
}

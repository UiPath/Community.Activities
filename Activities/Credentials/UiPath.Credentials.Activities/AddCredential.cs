using CredentialManagement;
using System.Activities;
using System.ComponentModel;

namespace UiPath.Credentials.Activities
{
    [Category("System.Credentials")]
    public class AddCredential : CodeActivity<bool>
    {
        [RequiredArgument]
        [Category("Input")]
        public InArgument<string> Username { get; set; }

        [RequiredArgument]
        [Category("Input")]
        public InArgument<string> Password { get; set; }

        [RequiredArgument]
        [Category("Input")]
        public InArgument<string> Target { get; set; }

        [Category("Input")]
        public CredentialType CredentialType { get; set; }

        [Category("Input")]
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

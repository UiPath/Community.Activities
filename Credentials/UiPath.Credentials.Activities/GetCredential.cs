using CredentialManagement;
using System.Activities;
using System.ComponentModel;

namespace UiPath.Credentials.Activities
{
    [Category("System.Credentials")]
    [Browsable(false)]
    public class GetCredential : CodeActivity<bool>
    {
        [RequiredArgument]
        [Category("Input")]
        public InArgument<string> Target { get; set; }

        [Category("Input")]
        public CredentialType CredentialType { get; set; }

        [Category("Input")]
        public PersistanceType PersistanceType { get; set; }

        [Category("Output")]
        public OutArgument<string> Username { get; set; }

        [Category("Output")]
        public OutArgument<string> Password { get; set; }

        public GetCredential()
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
            Password.Set(context, credential.Password);
            return true;
        }
    }
}

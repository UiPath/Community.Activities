using CredentialManagement;
using System.Activities;
using System.ComponentModel;

namespace UiPath.Credentials.Activities
{
    [Category("System.Credentials")]
    public class DeleteCredential : CodeActivity<bool>
    {
        [RequiredArgument]
        [Category("Input")]
        public InArgument<string> Target { get; set; }

        protected override bool Execute(CodeActivityContext context)
        {
            Credential credential = new Credential { Target = Target.Get(context) };
            return credential.Delete();
        }
    }
}

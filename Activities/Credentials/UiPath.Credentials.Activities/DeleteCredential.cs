using CredentialManagement;
using System.Activities;
using System.ComponentModel;
using UiPath.Credentials.Activities.Properties;

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
            Credential credential = new Credential { Target = Target.Get(context) };
            return credential.Delete();
        }
    }
}

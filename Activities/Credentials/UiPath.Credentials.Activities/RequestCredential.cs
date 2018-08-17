using CredentialManagement;
using System.Activities;
using System.ComponentModel;

namespace UiPath.Credentials.Activities
{
    [Category("System.Credentials")]
    public class RequestCredential : CodeActivity<bool>
    {
        [Category("Input")]
        public InArgument<string> Message { get; set; }

        [Category("Input")]
        public InArgument<string> Title { get; set; }


        [Category("Output")]
        public OutArgument<string> Username { get; set; }

        [Category("Output")]
        public OutArgument<string> Password { get; set; }

        protected override bool Execute(CodeActivityContext context)
        {
            var credPrompt = new VistaPrompt
            {
                GenericCredentials = true
            };
            var message = Message.Get(context);
            if (message != null)
            {
                credPrompt.Message = message;
            }

            var title = Title.Get(context);
            if (title != null)
            {
                credPrompt.Title = title;
            }

            var res = credPrompt.ShowDialog();
            if (res != DialogResult.OK) return false;

            Username.Set(context, credPrompt.Username);
            Password.Set(context, credPrompt.Password);
            return true;
        }
    }
}

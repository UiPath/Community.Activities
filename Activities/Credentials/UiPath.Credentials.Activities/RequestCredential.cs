using CredentialManagement;
using System.Activities;
using System.ComponentModel;
using System.Net;
using System.Security;
using UiPath.Credentials.Activities.Properties;

namespace UiPath.Credentials.Activities
{
    public class RequestCredential : CodeActivity<bool>
    {
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.MessageDisplayName))]
        [LocalizedDescription(nameof(Resources.MessageDescription))]
        public InArgument<string> Message { get; set; }

        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.TitleDisplayName))]
        [LocalizedDescription(nameof(Resources.TitleDescription))]
        public InArgument<string> Title { get; set; }


        [LocalizedCategory(nameof(Resources.Output))]
        [LocalizedDisplayName(nameof(Resources.UsernameDisplayName))]
        [LocalizedDescription(nameof(Resources.UsernameDescription))]
        public OutArgument<string> Username { get; set; }

        [LocalizedCategory(nameof(Resources.Output))]
        [LocalizedDisplayName(nameof(Resources.PasswordDisplayName))]
        [LocalizedDescription(nameof(Resources.PasswordDescription))]
        public OutArgument<string> Password { get; set; }

        [LocalizedCategory(nameof(Resources.Output))]
        [LocalizedDisplayName(nameof(Resources.PasswordSecureStringDisplayName))]
        [LocalizedDescription(nameof(Resources.PasswordSecureStringDescription))]
        public OutArgument<SecureString> PasswordSecureString { get; set; }

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
            PasswordSecureString.Set(context, (new NetworkCredential("", credPrompt.Password).SecurePassword));
            return true;
        }
    }
}

using CredentialManagement;
using System.Activities;
using System.Net;
using System.Security;
using UiPath.Credentials.Activities.Properties;
using UiPath.Shared.Activities;

#if ENABLE_DEFAULT_TELEMETRY
using UiPath.Shared.Telemetry.Services;
#endif

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
            ITelemetryOperationWrapper telemetryOperation = null;
#if ENABLE_DEFAULT_TELEMETRY
            telemetryOperation = RuntimeTelemetryService.CreateExecutionOperation(this, context);
#endif

            try
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

                telemetryOperation?.SetCustomDataKey(nameof(Message) + CredentialsConstants.IsUsed, message != null);
                telemetryOperation?.SetCustomDataKey(nameof(Message) + CredentialsConstants.Length, message != null ? message.Length : 0);
                telemetryOperation?.SetCustomDataKey(nameof(Title) + CredentialsConstants.IsUsed, title != null);
                telemetryOperation?.SetCustomDataKey(nameof(Title) + CredentialsConstants.Length, title != null ? title.Length : 0);

                var res = credPrompt.ShowDialog();
                if (res != DialogResult.OK) return false;

                Username.Set(context, credPrompt.Username);
                Password.Set(context, credPrompt.Password);
                PasswordSecureString.Set(context, (new NetworkCredential("", credPrompt.Password).SecurePassword));

                telemetryOperation?.Send();

                return true;
            }
            catch (System.Exception ex)
            {
                telemetryOperation?.SendWithException(ex);
                throw;
            }
        }
    }
}

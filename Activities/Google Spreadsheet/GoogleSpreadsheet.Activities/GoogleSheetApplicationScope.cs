using System;
using System.Activities;
using System.ComponentModel;
using System.Activities.Statements;

namespace GoogleSpreadsheet.Activities
{
    public class GoogleSheetApplicationScope : NativeActivity
    {
        [Browsable(false)]
        public ActivityAction<GoogleSheetProperty> Body { get; set; }

        [Category("Service Account Authentication")]
        public InArgument<string> ServiceAccountEmail { get; set; }

        [Category("Service Account Authentication")]
        public InArgument<string> KeyPath { get; set; }

        [Category("Service Account Authentication")]
        public InArgument<string> Password { get; set; }

        [Category("User Account Authentication")]
        public InArgument<string> CredentialID { get; set; }

        [Category("User Account Authentication")]
        public InArgument<string> CredentialSecret { get; set; }

        [Category("Key Authentication")]
        public InArgument<string> ApiKey { get; set; }

        [Category("Input")]
        [RequiredArgument]
        public InArgument<string> SpreadsheetId { get; set; }

        [Category("Input")]
        public GoogleAuthenticationType AuthenticationType { get; set; }

        internal static string GoogleSheetPropertyTag { get { return "GoogleSheetScope"; } }

        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            base.CacheMetadata(metadata);
            switch (AuthenticationType)
            {
                case GoogleAuthenticationType.Select:
                    metadata.AddValidationError("Please select an authentication type and fill in the appropriate fields");
                    break;
                case GoogleAuthenticationType.Token:
                    if (ApiKey == null)
                    {
                        metadata.AddValidationError("Please input API key field");
                    }
                    break;
                case GoogleAuthenticationType.OAuth2_User:
                    if (CredentialID == null || CredentialSecret == null)
                    {
                        metadata.AddValidationError("Please fill in CredentialId and CredentialSecret");
                    }
                    break;
                case GoogleAuthenticationType.OAuth2_ServiceAccount:
                    if (ServiceAccountEmail == null || KeyPath == null || Password == null)
                    {
                        metadata.AddValidationError("Please provide ServiceAccountEmail, json/p12 file path and password");
                    }
                    break;
                default:
                    break;
            }
        }

        public GoogleSheetApplicationScope()
        {
            AuthenticationType = 0;
            Body = new ActivityAction<GoogleSheetProperty>
            {
                Argument = new DelegateInArgument<GoogleSheetProperty>(GoogleSheetPropertyTag),
                Handler = new Sequence { DisplayName = "Do" }
            };
        }

        protected override void Execute(NativeActivityContext context)
        {

            string spreadsheetId = SpreadsheetId.Get(context);
            GoogleSheetProperty googleSheetProperty;

            switch (AuthenticationType)
            {
                case GoogleAuthenticationType.Token:
                    string apiKey = ApiKey.Get(context);
                    googleSheetProperty = new GoogleSheetProperty(apiKey, spreadsheetId);
                    break;
                case GoogleAuthenticationType.OAuth2_User:
                    string credentialID = CredentialID.Get(context);
                    string credentialSecret = CredentialSecret.Get(context);
                    googleSheetProperty = new GoogleSheetProperty(credentialID, credentialSecret, spreadsheetId);
                    break;
                case GoogleAuthenticationType.OAuth2_ServiceAccount:
                    string serviceAccountEmail = ServiceAccountEmail.Get(context);
                    string keyPath = KeyPath.Get(context);
                    string password = Password.Get(context);
                    googleSheetProperty = new GoogleSheetProperty(keyPath, password, serviceAccountEmail, spreadsheetId);
                    break;
                default:
                    googleSheetProperty = new GoogleSheetProperty();
                    break;
            }

            if (Body != null)
            {
                context.ScheduleAction<GoogleSheetProperty>(Body, googleSheetProperty, OnCompleted, OnFaulted);
            }
        }

        private void OnFaulted(NativeActivityFaultContext faultContext, Exception propagatedException, ActivityInstance propagatedFrom)
        {
            //TODO
        }

        private void OnCompleted(NativeActivityContext context, ActivityInstance completedInstance)
        {
            //TODO
        }
    }


}

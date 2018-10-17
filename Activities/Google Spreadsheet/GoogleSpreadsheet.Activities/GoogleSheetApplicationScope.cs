using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using System;
using System.Activities;
using System.ComponentModel;
using System.Security.Cryptography.X509Certificates;
using System.Activities.Statements;

namespace GoogleSpreadsheet.Activities
{
    public class GoogleSheetApplicationScope : NativeActivity
    {
        [Browsable(false)]
        public ActivityAction<GoogleSheetProperty> Body { get; set; }
        
        [Category("Service Account Authentication")]
        [RequiredArgument]
        [OverloadGroup("Service Account Authentication")]
        public InArgument<string> ServiceAccountEmail { get; set; } 

        [Category("Service Account Authentication")]
        [RequiredArgument]
        [OverloadGroup("Service Account Authentication")]
        public InArgument<string> KeyPath { get; set; } 

        [Category("Service Account Authentication")]
        [RequiredArgument]
        [OverloadGroup("Service Account Authentication")]
        public InArgument<string> Password { get; set; }

        [Category("Key Authentication")]
        [RequiredArgument]
        [OverloadGroup("Key Authentication")]
        public InArgument<string> ApiKey { get; set; }

        [Category("Input")]
        [RequiredArgument]
        public InArgument<string> SpreadsheetId { get; set; }

        internal static string GoogleSheetPropertyTag { get { return "GoogleSheetScope"; } }
        
        public GoogleSheetApplicationScope()
        {
            Body = new ActivityAction<GoogleSheetProperty>
            {
                Argument = new DelegateInArgument<GoogleSheetProperty>(GoogleSheetPropertyTag),
                Handler = new Sequence { DisplayName = "Do" }
            };
        }

        protected override void Execute(NativeActivityContext context)
        {
            string serviceAccountEmail = ServiceAccountEmail.Get(context);
            string keyPath = KeyPath.Get(context);
            string password = Password.Get(context);
            string apiKey = ApiKey.Get(context);
            string spreadsheetId = SpreadsheetId.Get(context);

            var googleSheetProperty = string.IsNullOrEmpty(apiKey) ? new GoogleSheetProperty(keyPath, password, serviceAccountEmail, spreadsheetId) : new GoogleSheetProperty(apiKey, spreadsheetId) ;

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

    public class GoogleSheetProperty
    {
        public SheetsService SheetsService { get; set; }

        public GoogleSheetProperty() {

        }

        public GoogleSheetProperty(string keyPath, string password, string serviceAccountEmail, string spreadsheetId)
        {
            SpreadsheetId = spreadsheetId;

            var certificate = new X509Certificate2(keyPath, password, X509KeyStorageFlags.Exportable);

            ServiceAccountCredential credential = new ServiceAccountCredential(
               new ServiceAccountCredential.Initializer(serviceAccountEmail)
               {
                   Scopes = new[] { SheetsService.Scope.Spreadsheets }
               }.FromCertificate(certificate));

            // Create the service.
            SheetsService = new SheetsService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "UiPath Robot",
            });
        }

        public GoogleSheetProperty(string apiKey, string spreadsheetId)
        {
            SpreadsheetId = spreadsheetId;

            // Create the service.
            SheetsService = new SheetsService(new BaseClientService.Initializer()
            {
                ApiKey = apiKey,
                ApplicationName = "UiPath Robot",
            });
        }

        public string SpreadsheetId { get; set; }
    }
}

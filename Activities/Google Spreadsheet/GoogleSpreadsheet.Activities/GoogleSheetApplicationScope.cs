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
        
        [Category("Authentication")]
        [RequiredArgument]
        public InArgument<string> ServiceAccountEmail { get; set; } 

        [Category("Authentication")]
        [RequiredArgument]
        public InArgument<string> KeyPath { get; set; } 

        [Category("Authentication")]
        [RequiredArgument]
        public InArgument<string> Password { get; set; } 

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
            
            var certificate = new X509Certificate2(@keyPath, password, X509KeyStorageFlags.Exportable);

            ServiceAccountCredential credential = new ServiceAccountCredential(
               new ServiceAccountCredential.Initializer(serviceAccountEmail)
               {
                   Scopes = new[] { SheetsService.Scope.Spreadsheets }
               }.FromCertificate(certificate));

            // Create the service.
            var sheetService = new SheetsService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "UiPath Robot",
            });

            var googleSheetProperty = new GoogleSheetProperty()
            {
                SheetsService = sheetService,
                SpreadsheetId = SpreadsheetId.Get(context)
            };

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

        public string SpreadsheetId { get; set; }
    }
}

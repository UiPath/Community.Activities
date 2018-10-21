using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.Threading;
using Google.Apis.Util.Store;

namespace GoogleSpreadsheet
{
    public enum GoogleAuthenticationType
    {
        Select,
        Token,
        OAuth2_User,
        OAuth2_ServiceAccount
    }

    public class GoogleSheetProperty
    {
        public SheetsService SheetsService { get; set; }
        public string SpreadsheetId { get; set; }

        private const string applicationName = "UiPath Robot";
        private const string tokenStoragePath = "Datastore.GoogleSpreadsheet";

        public GoogleSheetProperty()
        {

        }

        // Constructor for OAuth2 user account
        public GoogleSheetProperty(string clientID, string clientSecret, string spreadsheetId)
        {
            SpreadsheetId = spreadsheetId;

            UserCredential credential = AuthorizeUserCredential(clientID, clientSecret, new[] { SheetsService.Scope.Spreadsheets }, tokenStoragePath).Result;

            // Create the service.
            SheetsService = new SheetsService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = applicationName,
            });
        }

        // Constructor for OAuth2 service account
        public GoogleSheetProperty(string keyPath, string password, string serviceAccountEmail, string spreadsheetId)
        {
            SpreadsheetId = spreadsheetId;

            ServiceAccountCredential credential = AuthorizeServiceAccountCredential(keyPath, password, serviceAccountEmail, new[] { SheetsService.Scope.Spreadsheets });

            // Create the service.
            SheetsService = new SheetsService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = applicationName,
            });
        }

        // Constructor for Token authentication
        public GoogleSheetProperty(string apiKey, string spreadsheetId)
        {
            SpreadsheetId = spreadsheetId;

            // Create the service.
            SheetsService = new SheetsService(new BaseClientService.Initializer()
            {
                ApiKey = apiKey,
                ApplicationName = applicationName,
            });
        }

        private async Task<UserCredential> AuthorizeUserCredential(string clientID, string clientSecret, string[] scopes, string fileDataStorePath)
        {
            return await GoogleWebAuthorizationBroker.AuthorizeAsync(
                new ClientSecrets
                {
                    ClientId = clientID,
                    ClientSecret = clientSecret
                },
                scopes,
                "user",
                CancellationToken.None,
                new FileDataStore(fileDataStorePath));
        }

        private ServiceAccountCredential AuthorizeServiceAccountCredential(string keyPath, string password, string serviceAccountEmail, string[] scopes)
        {
            var certificate = new X509Certificate2(keyPath, password, X509KeyStorageFlags.Exportable);

            ServiceAccountCredential credential = new ServiceAccountCredential(
               new ServiceAccountCredential.Initializer(serviceAccountEmail)
               {
                   Scopes = scopes
               }.FromCertificate(certificate));

            return credential;
        }

    }
}

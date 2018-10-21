using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.Threading;
using Google.Apis.Util.Store;
using System;

namespace GoogleSpreadsheet
{
    public enum GoogleAuthenticationType
    {
        Select,
        ApiKey,
        OAuth2User,
        OAuth2ServiceAccount
    }

    public class GoogleSheetProperty
    {
        #region public members
        public SheetsService SheetsService { get; set; }
        public string SpreadsheetId { get; set; }
        #endregion

        #region public methods
        // Method for constructing token
        public static GoogleSheetProperty Create(string apiKey, string spreadsheetId)
        {
            var ret = new GoogleSheetProperty(spreadsheetId);
            return ret.InitializeApiKey(apiKey);
        }

        // Method for constructing OAuth2 User account
        public static Task<GoogleSheetProperty> Create(string clientID, string clientSecret, string spreadsheetId)
        {
            var ret = new GoogleSheetProperty(spreadsheetId);
            return ret.InitializeUserCredential(clientID, clientSecret);
        }

        // Method for constructing OAuth2 Service account
        public static GoogleSheetProperty Create(string keyPath, string password, string serviceAccountEmail, string spreadsheetId)
        {
            var ret = new GoogleSheetProperty(spreadsheetId);
            return ret.InitializeServiceAccount(keyPath, password, serviceAccountEmail);
        }
        #endregion
        #region private members
        private const string applicationName = "UiPath Robot";
        private const string tokenStoragePath = "Datastore.GoogleSpreadsheet";
        private ICredential credential;

        #endregion
        #region private methods
        private GoogleSheetProperty(string spreadsheetId)
        {
            SpreadsheetId = spreadsheetId;
        }

        private async Task<GoogleSheetProperty> InitializeUserCredential(string clientID, string clientSecret)
        {
            credential = await AuthorizeUserCredential(clientID, clientSecret, new[] { SheetsService.Scope.Spreadsheets }, tokenStoragePath, CancellationToken.None);
            await RefreshLocalToken((UserCredential)credential, CancellationToken.None);
            CreateService();
            return this;
        }

        private GoogleSheetProperty InitializeServiceAccount(string keyPath, string password, string serviceAccountEmail)
        {
            credential = AuthorizeServiceAccountCredential(keyPath, password, serviceAccountEmail, new[] { SheetsService.Scope.Spreadsheets });
            CreateService();
            return this;
        }

        private GoogleSheetProperty InitializeApiKey(string apiKey)
        {
            CreateService(apiKey);
            return this;
        }

        private void CreateService ()
        {
            // Create the service using the credentials obtained.
            SheetsService = new SheetsService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = applicationName,
            });
        }

        private void CreateService(string apiKey)
        {
            // Create the service using a provided api key.
            SheetsService = new SheetsService(new BaseClientService.Initializer()
            {
                ApiKey = apiKey,
                ApplicationName = applicationName,
            });
        }

        private async Task<bool> RefreshLocalToken(UserCredential credential, CancellationToken cancellationToken)
        {
            if (credential.Token.IsExpired(credential.Flow.Clock))
            {
                return await credential.RefreshTokenAsync(cancellationToken);
            }
            else
            {
                return await Task.FromResult(true);
            }
        }

        private async Task<UserCredential> AuthorizeUserCredential(string clientID, string clientSecret, string[] scopes, string fileDataStorePath, CancellationToken cancellationToken)
        {
            return await GoogleWebAuthorizationBroker.AuthorizeAsync(
                new ClientSecrets
                {
                    ClientId = clientID,
                    ClientSecret = clientSecret
                },
                scopes,
                "user",
                cancellationToken,
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
        #endregion
    }
}

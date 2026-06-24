using System;
using System.Net;
using System.Runtime.Versioning;
using System.Security;
using CredentialManagement;
using UiPath.Credentials.Activities.API.Models;

namespace UiPath.Credentials.Activities.API
{
    [SupportedOSPlatform("windows")]
    internal class CredentialsService : ICredentialsService
    {
        public GetCredentialResult GetCredential(string target, CredentialType credentialType = CredentialType.Generic, PersistanceType persistanceType = PersistanceType.Enterprise)
        {
            if (string.IsNullOrWhiteSpace(target))
                throw new ArgumentException("Target must not be null or empty.", nameof(target));

            var credential = new Credential { Target = target, Type = credentialType, PersistanceType = persistanceType };
            var found = credential.Load();

            return found
                ? new GetCredentialResult(true, credential.Username, credential.Password)
                : new GetCredentialResult(false, null, null);
        }

        public GetSecureCredentialResult GetSecureCredential(string target, CredentialType credentialType = CredentialType.Generic, PersistanceType persistanceType = PersistanceType.Enterprise)
        {
            if (string.IsNullOrWhiteSpace(target))
                throw new ArgumentException("Target must not be null or empty.", nameof(target));

            var credential = new Credential { Target = target, Type = credentialType, PersistanceType = persistanceType };
            var found = credential.Load();

            return found
                ? new GetSecureCredentialResult(true, credential.Username, credential.SecurePassword)
                : new GetSecureCredentialResult(false, null, null);
        }

        public bool AddCredential(string target, string username, string password, CredentialType credentialType = CredentialType.Generic, PersistanceType persistanceType = PersistanceType.Enterprise)
        {
            if (string.IsNullOrWhiteSpace(target))
                throw new ArgumentException("Target must not be null or empty.", nameof(target));
            ArgumentNullException.ThrowIfNull(password);

            var credential = new Credential
            {
                Target = target,
                Username = username,
                Password = password,
                Type = credentialType,
                PersistanceType = persistanceType
            };

            return credential.Save();
        }

        public bool AddCredential(string target, string username, SecureString password, CredentialType credentialType = CredentialType.Generic, PersistanceType persistanceType = PersistanceType.Enterprise)
        {
            if (string.IsNullOrWhiteSpace(target))
                throw new ArgumentException("Target must not be null or empty.", nameof(target));
            ArgumentNullException.ThrowIfNull(password);

            var credential = new Credential
            {
                Target = target,
                Username = username,
                Password = new NetworkCredential(string.Empty, password).Password,
                Type = credentialType,
                PersistanceType = persistanceType
            };

            return credential.Save();
        }

        public bool DeleteCredential(string target)
        {
            if (string.IsNullOrWhiteSpace(target))
                throw new ArgumentException("Target must not be null or empty.", nameof(target));

            var credential = new Credential { Target = target };
            return credential.Delete();
        }

        public RequestCredentialResult RequestCredential(string message = null, string title = null)
        {
            var prompt = new VistaPrompt { GenericCredentials = true };

            if (message != null)
                prompt.Message = message;
            if (title != null)
                prompt.Title = title;

            var result = prompt.ShowDialog();
            if (result != DialogResult.OK)
                return new RequestCredentialResult(false, null, null);

            var securePassword = new NetworkCredential(string.Empty, prompt.Password).SecurePassword;
            return new RequestCredentialResult(true, prompt.Username, securePassword);
        }
    }
}

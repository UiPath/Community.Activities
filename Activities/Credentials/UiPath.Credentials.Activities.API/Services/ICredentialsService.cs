using System.Runtime.Versioning;
using System.Security;
using CredentialManagement;
using UiPath.Credentials.Activities.API.Models;

namespace UiPath.Credentials.Activities.API
{
    /// <summary>
    /// Provides Windows Credential Manager capabilities for coded workflows.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public interface ICredentialsService
    {
        /// <summary>
        /// Retrieves a credential from the Windows Credential Manager.
        /// </summary>
        /// <param name="target">The name of the credential entry.</param>
        /// <param name="credentialType">The type of credential to retrieve.</param>
        /// <param name="persistanceType">The persistence scope of the credential.</param>
        /// <returns>
        /// A result containing <c>Found = true</c> and the <c>Username</c>/<c>Password</c> if the entry exists;
        /// otherwise <c>Found = false</c>.
        /// </returns>
        GetCredentialResult GetCredential(string target, CredentialType credentialType = CredentialType.Generic, PersistanceType persistanceType = PersistanceType.Enterprise);

        /// <summary>
        /// Retrieves a credential from the Windows Credential Manager, returning the password as a <see cref="SecureString"/>.
        /// </summary>
        /// <param name="target">The name of the credential entry.</param>
        /// <param name="credentialType">The type of credential to retrieve.</param>
        /// <param name="persistanceType">The persistence scope of the credential.</param>
        /// <returns>
        /// A result containing <c>Found = true</c> and the <c>Username</c>/<c>Password</c> if the entry exists;
        /// otherwise <c>Found = false</c>.
        /// </returns>
        GetSecureCredentialResult GetSecureCredential(string target, CredentialType credentialType = CredentialType.Generic, PersistanceType persistanceType = PersistanceType.Enterprise);

        /// <summary>
        /// Adds or updates a credential in the Windows Credential Manager.
        /// </summary>
        /// <param name="target">The name of the credential entry.</param>
        /// <param name="username">The username to store.</param>
        /// <param name="password">The plain-text password to store.</param>
        /// <param name="credentialType">The type of credential.</param>
        /// <param name="persistanceType">The persistence scope of the credential.</param>
        /// <returns><c>true</c> if the credential was saved successfully.</returns>
        bool AddCredential(string target, string username, string password, CredentialType credentialType = CredentialType.Generic, PersistanceType persistanceType = PersistanceType.Enterprise);

        /// <summary>
        /// Adds or updates a credential in the Windows Credential Manager using a <see cref="SecureString"/> password.
        /// </summary>
        /// <param name="target">The name of the credential entry.</param>
        /// <param name="username">The username to store.</param>
        /// <param name="password">The password to store as a <see cref="SecureString"/>.</param>
        /// <param name="credentialType">The type of credential.</param>
        /// <param name="persistanceType">The persistence scope of the credential.</param>
        /// <returns><c>true</c> if the credential was saved successfully.</returns>
        bool AddCredential(string target, string username, SecureString password, CredentialType credentialType = CredentialType.Generic, PersistanceType persistanceType = PersistanceType.Enterprise);

        /// <summary>
        /// Deletes a credential from the Windows Credential Manager.
        /// </summary>
        /// <param name="target">The name of the credential entry to delete.</param>
        /// <returns><c>true</c> if the credential was deleted successfully.</returns>
        bool DeleteCredential(string target);

        /// <summary>
        /// Displays the Windows credential prompt and returns what the user entered.
        /// </summary>
        /// <param name="message">Optional message displayed in the prompt.</param>
        /// <param name="title">Optional title of the prompt dialog.</param>
        /// <returns>
        /// A result containing <c>Confirmed = true</c> and the entered credentials if the user confirmed;
        /// otherwise <c>Confirmed = false</c>.
        /// </returns>
        /// <remarks>
        /// <b>Interactive use only.</b> This method shows a blocking Win32 dialog (CredUIPromptForWindowsCredentials).
        /// It must not be called from unattended robots, Windows services, or any context without an interactive
        /// desktop session — doing so will either hang indefinitely or throw a platform exception.
        /// </remarks>
        RequestCredentialResult RequestCredential(string message = null, string title = null);
    }
}

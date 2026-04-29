using System.Security;

namespace UiPath.Credentials.Activities.API.Models
{
    /// <summary>Result of a <see cref="ICredentialsService.GetSecureCredential"/> call.</summary>
    public record GetSecureCredentialResult(bool Found, string Username, SecureString Password);
}

using System.Security;

namespace UiPath.Credentials.Activities.API.Models
{
    /// <summary>Result of a <see cref="ICredentialsService.RequestCredential"/> call.</summary>
    public record RequestCredentialResult(bool Confirmed, string Username, SecureString Password);
}

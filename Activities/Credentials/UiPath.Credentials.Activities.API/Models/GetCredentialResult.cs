namespace UiPath.Credentials.Activities.API.Models
{
    /// <summary>Result of a <see cref="ICredentialsService.GetCredential"/> call.</summary>
    public record GetCredentialResult(bool Found, string Username, string Password);
}

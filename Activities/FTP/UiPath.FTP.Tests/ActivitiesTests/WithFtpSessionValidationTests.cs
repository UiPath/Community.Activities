using System;
using System.Activities;
using System.Activities.Validation;
using System.Linq;
using UiPath.FTP.Activities;
using Xunit;
using FtpRes = UiPath.FTP.Activities.Properties.Resources;

namespace UiPath.FTP.Tests
{
    /// <summary>
    /// Coverage for the design-time validation errors emitted by
    /// <see cref="WithFtpSession.CacheMetadata"/>. The runtime already enforces these
    /// constraints (<see cref="WithFtpSession.ExecuteAsync"/> / <see cref="SftpSession"/>); these
    /// tests pin the design-time surface so a refactor cannot quietly drop an error the studio
    /// author depends on.
    /// </summary>
    public class WithFtpSessionValidationTests
    {
        // SFTP has no concept of anonymous login: SftpSession always requires Username plus a
        // password/keyboard-interactive or private-key credential, and ExecuteAsync never
        // populates Username when UseAnonymousLogin is on. That combination reaches OpenAsync
        // and always throws NoValidAuthenticationMethod. CacheMetadata must catch it at design
        // time instead of letting the workflow run and fail every time.
        [Fact]
        public void UseSftpWithAnonymousLogin_EmitsValidationError()
        {
            var activity = new WithFtpSession
            {
                Host = new InArgument<string>("host"),
                UseSftp = true,
                UseAnonymousLogin = true,
            };

            ValidationError[] errors = ValidateAndGetErrors(activity);

            Assert.Contains(errors, e =>
                e.Message == FtpRes.AnonymousLoginNotSupportedOnSftp
                && e.PropertyName == nameof(WithFtpSession.UseAnonymousLogin));
        }

        // The error must fire regardless of whether a password/certificate is also configured:
        // Username is never populated when UseAnonymousLogin is on, so the combination is broken
        // independently of what other authentication properties are set.
        [Fact]
        public void UseSftpWithAnonymousLogin_EmitsValidationError_EvenWithPasswordConfigured()
        {
            var activity = new WithFtpSession
            {
                Host = new InArgument<string>("host"),
                UseSftp = true,
                UseAnonymousLogin = true,
                Password = new InArgument<string>("irrelevant"),
            };

            ValidationError[] errors = ValidateAndGetErrors(activity);

            Assert.Contains(errors, e => e.Message == FtpRes.AnonymousLoginNotSupportedOnSftp);
        }

        // FTP/FTPS supports anonymous login (FtpSession skips setting credentials and relies on
        // the underlying library's anonymous default), so the combination must not be flagged.
        [Fact]
        public void UseFtpWithAnonymousLogin_NoValidationError()
        {
            var activity = new WithFtpSession
            {
                Host = new InArgument<string>("host"),
                UseSftp = false,
                UseAnonymousLogin = true,
            };

            ValidationError[] errors = ValidateAndGetErrors(activity);

            Assert.DoesNotContain(errors, e => e.Message == FtpRes.AnonymousLoginNotSupportedOnSftp);
        }

        // SFTP with a real (non-anonymous) login must not be flagged by this check.
        [Fact]
        public void UseSftpWithoutAnonymousLogin_NoValidationError()
        {
            var activity = new WithFtpSession
            {
                Host = new InArgument<string>("host"),
                UseSftp = true,
                UseAnonymousLogin = false,
                Username = new InArgument<string>("user"),
                Password = new InArgument<string>("pass"),
            };

            ValidationError[] errors = ValidateAndGetErrors(activity);

            Assert.DoesNotContain(errors, e => e.Message == FtpRes.AnonymousLoginNotSupportedOnSftp);
        }

        // ActivityValidationServices.Validate runs CacheMetadata on the activity tree and
        // collects every error/warning into the returned ValidationResults. Then we filter to
        // errors only (isWarning=false entries).
        private static ValidationError[] ValidateAndGetErrors(Activity activity) =>
            ActivityValidationServices.Validate(activity).Errors.ToArray();
    }
}

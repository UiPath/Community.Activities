using System;
using System.Activities;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Security;
using UiPath.Cryptography.Activities.Models;
using UiPath.Cryptography.Activities.Properties;
using UiPath.Cryptography.Enums;
using UiPath.Platform.ResourceHandling;
using UiPath.Shared.Activities;
#if ENABLE_DEFAULT_TELEMETRY
using UiPath.Shared.Telemetry.Services;
#endif

#pragma warning disable CS0618 // CryptographyHelper is intentionally marked Obsolete to discourage external use; in-package consumers are expected.

namespace UiPath.Cryptography.Activities
{
    [LocalizedDisplayName(nameof(Resources.Activity_PgpGenerateKeys_Name))]
    [LocalizedDescription(nameof(Resources.Activity_PgpGenerateKeys_Description))]
    public partial class PgpGenerateKeys : CodeActivity
    {
        [RequiredArgument]
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_PgpGenerateKeys_Property_PublicKeyFilePath_Name))]
        [LocalizedDescription(nameof(Resources.Activity_PgpGenerateKeys_Property_PublicKeyFilePath_Description))]
        public InArgument<string> PublicKeyFilePath { get; set; }

        [RequiredArgument]
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_PgpGenerateKeys_Property_PrivateKeyFilePath_Name))]
        [LocalizedDescription(nameof(Resources.Activity_PgpGenerateKeys_Property_PrivateKeyFilePath_Description))]
        public InArgument<string> PrivateKeyFilePath { get; set; }

        [RequiredArgument]
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_PgpGenerateKeys_Property_UserId_Name))]
        [LocalizedDescription(nameof(Resources.Activity_PgpGenerateKeys_Property_UserId_Description))]
        public InArgument<string> UserId { get; set; }

        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_PgpGenerateKeys_Property_Password_Name))]
        [LocalizedDescription(nameof(Resources.Activity_PgpGenerateKeys_Property_Password_Description))]
        public InArgument<string> Passphrase { get; set; }

        [DefaultValue(null)]
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_PgpGenerateKeys_Property_PassphraseSecureString_Name))]
        [LocalizedDescription(nameof(Resources.Activity_PgpGenerateKeys_Property_PassphraseSecureString_Description))]
        public InArgument<SecureString> PassphraseSecureString { get; set; }

        [DefaultValue(false)]
        [LocalizedCategory(nameof(Resources.Category_Options_Name))]
        [LocalizedDisplayName(nameof(Resources.Activity_PgpGenerateKeys_Property_Overwrite_Name))]
        [LocalizedDescription(nameof(Resources.Activity_PgpGenerateKeys_Property_Overwrite_Description))]
        public InArgument<bool> Overwrite { get; set; }

        [DefaultValue(RsaKeySize.Rsa4096)]
        [LocalizedCategory(nameof(Resources.Category_Options_Name))]
        [LocalizedDisplayName(nameof(Resources.Activity_PgpGenerateKeys_Property_KeySize_Name))]
        [LocalizedDescription(nameof(Resources.Activity_PgpGenerateKeys_Property_KeySize_Description))]
        public InArgument<RsaKeySize> KeySize { get; set; }

        [DefaultValue(null)]
        [LocalizedCategory(nameof(Resources.Category_Options_Name))]
        [LocalizedDisplayName(nameof(Resources.Activity_PgpGenerateKeys_Property_ContinueOnError_Name))]
        [LocalizedDescription(nameof(Resources.Activity_PgpGenerateKeys_Property_ContinueOnError_Description))]
        public InArgument<bool> ContinueOnError { get; set; }

        [Browsable(false)]
        [DefaultValue(null)]
        [LocalizedCategory(nameof(Resources.Output))]
        [LocalizedDisplayName(nameof(Resources.Activity_PgpGenerateKeys_Property_PublicKeyFile_Name))]
        [LocalizedDescription(nameof(Resources.Activity_PgpGenerateKeys_Property_PublicKeyFile_Description))]
        public OutArgument<ILocalResource> PublicKeyFile { get; set; }

        [Browsable(false)]
        [DefaultValue(null)]
        [LocalizedCategory(nameof(Resources.Output))]
        [LocalizedDisplayName(nameof(Resources.Activity_PgpGenerateKeys_Property_PrivateKeyFile_Name))]
        [LocalizedDescription(nameof(Resources.Activity_PgpGenerateKeys_Property_PrivateKeyFile_Description))]
        public OutArgument<ILocalResource> PrivateKeyFile { get; set; }

        protected override void Execute(CodeActivityContext context)
        {
#if ENABLE_DEFAULT_TELEMETRY
            var telemetryOperation = RuntimeTelemetryService.CreateExecutionOperation(this, context);
#endif

            try
            {
                var publicKeyFilePath = PublicKeyFilePath.Get(context);
                var privateKeyFilePath = PrivateKeyFilePath.Get(context);
                var userId = UserId.Get(context);

                if (string.IsNullOrWhiteSpace(publicKeyFilePath))
                    throw new ArgumentNullException(Resources.Activity_PgpGenerateKeys_Property_PublicKeyFilePath_Name);
                if (string.IsNullOrWhiteSpace(privateKeyFilePath))
                    throw new ArgumentNullException(Resources.Activity_PgpGenerateKeys_Property_PrivateKeyFilePath_Name);
                if (string.IsNullOrWhiteSpace(userId))
                    throw new ArgumentNullException(Resources.Activity_PgpGenerateKeys_Property_UserId_Name);

                var passwordString = Passphrase.Get(context);
                if (string.IsNullOrWhiteSpace(passwordString))
                {
                    var secure = PassphraseSecureString.Get(context);
                    if (secure == null || secure.Length == 0)
                        throw new ArgumentNullException(nameof(Passphrase), Resources.Activity_PgpGenerateKeys_Property_Password_Name);
                    passwordString = new NetworkCredential(string.Empty, secure).Password;
                }

                if (!Overwrite.Get(context))
                {
                    if (File.Exists(publicKeyFilePath))
                        throw new ArgumentException(Resources.FileAlreadyExistsException, Resources.Activity_PgpGenerateKeys_Property_PublicKeyFilePath_Name);
                    if (File.Exists(privateKeyFilePath))
                        throw new ArgumentException(Resources.FileAlreadyExistsException, Resources.Activity_PgpGenerateKeys_Property_PrivateKeyFilePath_Name);
                }

                var pubDir = Path.GetDirectoryName(publicKeyFilePath);
                if (!string.IsNullOrEmpty(pubDir))
                    Directory.CreateDirectory(pubDir);

                var privDir = Path.GetDirectoryName(privateKeyFilePath);
                if (!string.IsNullOrEmpty(privDir))
                    Directory.CreateDirectory(privDir);

                var keySizeValue = this.KeySize.Get(context);
                if (keySizeValue == 0)
                    keySizeValue = RsaKeySize.Rsa4096;
                CryptographyHelper.PgpGenerateKeys(publicKeyFilePath, privateKeyFilePath, userId, passwordString, keySizeValue);

                var pubItem = new CryptographyLocalItem(Path.GetFileName(publicKeyFilePath), publicKeyFilePath);
                PublicKeyFile.Set(context, pubItem);

                var privItem = new CryptographyLocalItem(Path.GetFileName(privateKeyFilePath), privateKeyFilePath);
                PrivateKeyFile.Set(context, privItem);

#if ENABLE_DEFAULT_TELEMETRY
                telemetryOperation.Send();
#endif
            }
            catch (Exception ex)
            {
#if ENABLE_DEFAULT_TELEMETRY
                telemetryOperation.SendWithException(ex);
#endif
                Trace.TraceError(ex.ToString());
                if (!ContinueOnError.Get(context)) throw;
            }
        }
    }
}

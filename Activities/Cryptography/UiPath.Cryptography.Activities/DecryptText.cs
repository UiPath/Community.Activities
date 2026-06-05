using System;
using System.Activities;
using System.Activities.Validation;
using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using UiPath.Cryptography.Activities.Helpers;
using UiPath.Cryptography.Activities.Properties;
using UiPath.Cryptography.Enums;
using UiPath.Platform.ResourceHandling;
using UiPath.Shared.Activities;
#if ENABLE_DEFAULT_TELEMETRY
using UiPath.Shared.Telemetry.Services;
#endif

#pragma warning disable CS0618 // obsolete encryption algorithms (TripleDES, etc.) remain referenced for backwards compatibility

namespace UiPath.Cryptography.Activities
{
    [LocalizedDisplayName(nameof(Resources.Activity_DecryptText_Name))]
    [LocalizedDescription(nameof(Resources.Activity_DecryptText_Description))]
    public partial class DecryptText : CodeActivity<string>
    {
        [RequiredArgument]
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_DecryptText_Property_Algorithm_Name))]
        [LocalizedDescription(nameof(Resources.Activity_DecryptText_Property_Algorithm_Description))]
        public EncryptionAlgorithm Algorithm { get; set; }

        [RequiredArgument]
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_DecryptText_Property_Input_Name))]
        [LocalizedDescription(nameof(Resources.Activity_DecryptText_Property_Input_Description))]
        public InArgument<string> Input { get; set; }

        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_DecryptText_Property_Key_Name))]
        [LocalizedDescription(nameof(Resources.Activity_DecryptText_Property_Key_Description))]
        public InArgument<string> Key { get; set; }

        [Browsable(false)]
        [Obsolete("Legacy property kept for XAML back-compat with workflows that persisted the active key input mode. The activity now infers the mode from which side is bound.")]
        public KeyInputMode KeyInputModeSwitch { get; set; }

        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_DecryptText_Property_KeySecureString_Name))]
        [LocalizedDescription(nameof(Resources.Activity_DecryptText_Property_KeySecureString_Description))]
        public InArgument<SecureString> KeySecureString { get; set; }

        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_DecryptText_Property_Encoding_Name))]
        [LocalizedDescription(nameof(Resources.Activity_DecryptText_Property_Encoding_Description))]
        public InArgument<Encoding> Encoding { get; set; }

        [Browsable(false)]
        public InArgument<string> KeyEncodingString { get; set; }

        [LocalizedCategory(nameof(Resources.Output))]
        [LocalizedDisplayName(nameof(Resources.Activity_DecryptText_Property_Result_Name))]
        [LocalizedDescription(nameof(Resources.Activity_DecryptText_Property_Result_Description))]
        public new OutArgument<string> Result { get => base.Result; set => base.Result = value; }

        [DefaultValue(null)]
        [LocalizedCategory(nameof(Resources.Category_Options_Name))]
        [LocalizedDisplayName(nameof(Resources.Activity_DecryptText_Property_ContinueOnError_Name))]
        [LocalizedDescription(nameof(Resources.Activity_DecryptText_Property_ContinueOnError_Description))]
        public InArgument<bool> ContinueOnError { get; set; }

        [DefaultValue(null)]
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_DecryptText_Property_PrivateKeyFilePath_Name))]
        [LocalizedDescription(nameof(Resources.Activity_DecryptText_Property_PrivateKeyFilePath_Description))]
        public InArgument<string> PrivateKeyFilePath { get; set; }

        [DefaultValue(null)]
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_DecryptText_Property_Passphrase_Name))]
        [LocalizedDescription(nameof(Resources.Activity_DecryptText_Property_Passphrase_Description))]
        public InArgument<string> Passphrase { get; set; }

        [DefaultValue(null)]
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_DecryptText_Property_PassphraseSecureString_Name))]
        [LocalizedDescription(nameof(Resources.Activity_DecryptText_Property_PassphraseSecureString_Description))]
        public InArgument<SecureString> PassphraseSecureString { get; set; }

        [DefaultValue(false)]
        [LocalizedCategory(nameof(Resources.Category_Options_Name))]
        [LocalizedDisplayName(nameof(Resources.Activity_DecryptText_Property_VerifySignature_Name))]
        [LocalizedDescription(nameof(Resources.Activity_DecryptText_Property_VerifySignature_Description))]
        public bool VerifySignature { get; set; }

        [DefaultValue(null)]
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_DecryptText_Property_PublicKeyFilePath_Name))]
        [LocalizedDescription(nameof(Resources.Activity_DecryptText_Property_PublicKeyFilePath_Description))]
        public InArgument<string> PublicKeyFilePath { get; set; }

        [Browsable(false)]
        [DefaultValue(null)]
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_DecryptText_Property_PublicKeyFile_Name))]
        [LocalizedDescription(nameof(Resources.Activity_DecryptText_Property_PublicKeyFile_Description))]
        public InArgument<IResource> PublicKeyFile { get; set; }

        public DecryptText()
        {
            Algorithm = EncryptionAlgorithm.AESGCM;
            KeyEncodingString = System.Text.Encoding.UTF8.CodePage.ToString();
        }

        protected override void CacheMetadata(CodeActivityMetadata metadata)
        {
            base.CacheMetadata(metadata);

            if (!CryptographyHelper.IsFipsCompliant(Algorithm))
            {
                metadata.AddValidationError(new ValidationError(Resources.FipsComplianceWarning, isWarning: true, nameof(Algorithm)));
            }

            if (Algorithm == EncryptionAlgorithm.ChaCha20Poly1305 && !System.Security.Cryptography.ChaCha20Poly1305.IsSupported)
            {
                metadata.AddValidationError(new ValidationError(Resources.ChaCha20Poly1305NotSupported, isWarning: true, nameof(Algorithm)));
            }
        }

        protected override string Execute(CodeActivityContext context)
        {
#if ENABLE_DEFAULT_TELEMETRY
            var telemetryOperation = RuntimeTelemetryService.CreateExecutionOperation(this, context);
#endif

            string result = null;
            try
            {
                var input = Input.Get(context);

                if (string.IsNullOrWhiteSpace(input))
                    throw new ArgumentNullException(Resources.InputStringDisplayName);

                result = Algorithm == EncryptionAlgorithm.PGP
                    ? ExecutePgpDecrypt(context, input)
                    : ExecuteSymmetricDecrypt(context, input);

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

                if (!ContinueOnError.Get(context))
                {
                    throw;
                }
            }
            return result;
        }

        private string ExecutePgpDecrypt(CodeActivityContext context, string input)
        {
            var privateKeyFilePath = PrivateKeyFilePath.Get(context);

            var passphraseString = Passphrase.Get(context);
            if (string.IsNullOrWhiteSpace(passphraseString))
            {
                var secure = PassphraseSecureString.Get(context);
                if (secure == null || secure.Length == 0)
                    throw new ArgumentNullException(nameof(Passphrase), Resources.Activity_DecryptText_Property_Passphrase_Name);
                passphraseString = new NetworkCredential(string.Empty, secure).Password;
            }

            var publicKeyFilePath = PublicKeyFilePath.Get(context);
            var publicKeyResource = PublicKeyFile?.Get(context);
            if (string.IsNullOrEmpty(publicKeyFilePath) && publicKeyResource != null)
            {
                var localResource = publicKeyResource.ToLocalResource();
                localResource.ResolveAsync().GetAwaiter().GetResult();
                publicKeyFilePath = localResource.LocalPath;
            }

            return PgpStreamHelper.WithPgpDecryptStreams(
                privateKeyFilePath, passphraseString, publicKeyFilePath, VerifySignature,
                (privStream, pass, pubStream) =>
                    CryptographyHelper.PgpDecryptText(input, privStream, pass, pubStream, VerifySignature));
        }

        private string ExecuteSymmetricDecrypt(CodeActivityContext context, string input)
        {
            var key = Key.Get(context);
            var keySecureString = KeySecureString.Get(context);
            var keyEncoding = Encoding.Get(context);
            var keyEncodingString = KeyEncodingString.Get(context);

            if (string.IsNullOrWhiteSpace(key))
            {
                if (keySecureString == null || keySecureString.Length == 0)
                    throw new ArgumentNullException(nameof(Key), Resources.Activity_DecryptText_Property_Key_Name);
                key = null; // ensure helper falls back to SecureString
            }
            if (keyEncoding == null && string.IsNullOrEmpty(keyEncodingString))
                throw new ArgumentNullException(Resources.Encoding);

            keyEncoding = EncodingHelpers.KeyEncodingOrString(keyEncoding, keyEncodingString);

            byte[] decrypted;
            try
            {
                decrypted = CryptographyHelper.DecryptData(Algorithm, Convert.FromBase64String(input), CryptographyHelper.KeyEncoding(keyEncoding, key, keySecureString));
            }
            catch (CryptographicException ex)
            {
                throw new InvalidOperationException(Resources.GenericCryptographicException, ex);
            }

            return keyEncoding.GetString(decrypted);
        }
    }
}

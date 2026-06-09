using System;
using System.Activities;
using System.Activities.Validation;
using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Security;
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
    [LocalizedDisplayName(nameof(Resources.Activity_EncryptText_Name))]
    [LocalizedDescription(nameof(Resources.Activity_EncryptText_Description))]
    public partial class EncryptText : CodeActivity<string>
    {
        [RequiredArgument]
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_EncryptText_Property_Algorithm_Name))]
        [LocalizedDescription(nameof(Resources.Activity_EncryptText_Property_Algorithm_Description))]
        public EncryptionAlgorithm Algorithm { get; set; }

        [RequiredArgument]
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_EncryptText_Property_Input_Name))]
        [LocalizedDescription(nameof(Resources.Activity_EncryptText_Property_Input_Description))]
        public InArgument<string> Input { get; set; }

        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_EncryptText_Property_Key_Name))]
        [LocalizedDescription(nameof(Resources.Activity_EncryptText_Property_Key_Description))]
        public InArgument<string> Key { get; set; }

        [Browsable(false)]
        [Obsolete("Legacy property kept for XAML back-compat with workflows that persisted the active key input mode. The activity now infers the mode from which side is bound.")]
        public KeyInputMode KeyInputModeSwitch { get; set; }

        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_EncryptText_Property_KeySecureString_Name))]
        [LocalizedDescription(nameof(Resources.Activity_EncryptText_Property_KeySecureString_Description))]
        public InArgument<SecureString> KeySecureString { get; set; }

        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_EncryptText_Property_Encoding_Name))]
        [LocalizedDescription(nameof(Resources.Activity_EncryptText_Property_Encoding_Description))]
        public InArgument<Encoding> Encoding { get; set; }

        [Browsable(false)]
        public InArgument<string> KeyEncodingString { get; set; }

        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_EncryptText_Property_Format_Name))]
        [LocalizedDescription(nameof(Resources.Activity_EncryptText_Property_Format_Description))]
        [DefaultValue(SymmetricWireFormat.Classic)]
        public SymmetricWireFormat Format { get; set; }

        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_EncryptText_Property_KeyFormat_Name))]
        [LocalizedDescription(nameof(Resources.Activity_EncryptText_Property_KeyFormat_Description))]
        [DefaultValue(KeyBytesFormat.Encoded)]
        public KeyBytesFormat KeyFormat { get; set; }

        [DefaultValue(null)]
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_EncryptText_Property_Iv_Name))]
        [LocalizedDescription(nameof(Resources.Activity_EncryptText_Property_Iv_Description))]
        public InArgument<string> Iv { get; set; }

        [DefaultValue(null)]
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_EncryptText_Property_KdfIterations_Name))]
        [LocalizedDescription(nameof(Resources.Activity_EncryptText_Property_KdfIterations_Description))]
        public InArgument<int> KdfIterations { get; set; }

        [LocalizedCategory(nameof(Resources.Output))]
        [LocalizedDisplayName(nameof(Resources.Activity_EncryptText_Property_Result_Name))]
        [LocalizedDescription(nameof(Resources.Activity_EncryptText_Property_Result_Description))]
        public new OutArgument<string> Result { get => base.Result; set => base.Result = value; }

        [DefaultValue(null)]
        [LocalizedCategory(nameof(Resources.Category_Options_Name))]
        [LocalizedDisplayName(nameof(Resources.Activity_EncryptText_Property_ContinueOnError_Name))]
        [LocalizedDescription(nameof(Resources.Activity_EncryptText_Property_ContinueOnError_Description))]
        public InArgument<bool> ContinueOnError { get; set; }

        [DefaultValue(null)]
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_EncryptText_Property_PublicKeyFilePath_Name))]
        [LocalizedDescription(nameof(Resources.Activity_EncryptText_Property_PublicKeyFilePath_Description))]
        public InArgument<string> PublicKeyFilePath { get; set; }

        [Browsable(false)]
        [DefaultValue(null)]
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_EncryptText_Property_PublicKeyFile_Name))]
        [LocalizedDescription(nameof(Resources.Activity_EncryptText_Property_PublicKeyFile_Description))]
        public InArgument<IResource> PublicKeyFile { get; set; }

        [DefaultValue(false)]
        [LocalizedCategory(nameof(Resources.Category_Options_Name))]
        [LocalizedDisplayName(nameof(Resources.Activity_EncryptText_Property_SignData_Name))]
        [LocalizedDescription(nameof(Resources.Activity_EncryptText_Property_SignData_Description))]
        public bool SignData { get; set; }

        [DefaultValue(null)]
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_EncryptText_Property_PrivateKeyFilePath_Name))]
        [LocalizedDescription(nameof(Resources.Activity_EncryptText_Property_PrivateKeyFilePath_Description))]
        public InArgument<string> PrivateKeyFilePath { get; set; }

        [DefaultValue(null)]
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_EncryptText_Property_Passphrase_Name))]
        [LocalizedDescription(nameof(Resources.Activity_EncryptText_Property_Passphrase_Description))]
        public InArgument<string> Passphrase { get; set; }

        [DefaultValue(null)]
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_EncryptText_Property_PassphraseSecureString_Name))]
        [LocalizedDescription(nameof(Resources.Activity_EncryptText_Property_PassphraseSecureString_Description))]
        public InArgument<SecureString> PassphraseSecureString { get; set; }

        public EncryptText()
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

            if (Iv != null)
            {
                metadata.AddValidationError(new ValidationError(Resources.Iv_NonceReuseWarning, isWarning: true, nameof(Iv)));
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
                    ? ExecutePgpEncrypt(context, input)
                    : ExecuteSymmetricEncrypt(context, input);

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

        private string ExecutePgpEncrypt(CodeActivityContext context, string input)
        {
            var publicKeyFilePath = PublicKeyFilePath.Get(context);
            var publicKeyResource = PublicKeyFile?.Get(context);
            if (string.IsNullOrEmpty(publicKeyFilePath) && publicKeyResource != null)
            {
                var localResource = publicKeyResource.ToLocalResource();
                localResource.ResolveAsync().GetAwaiter().GetResult();
                publicKeyFilePath = localResource.LocalPath;
            }
            var privateKeyFilePath = PrivateKeyFilePath.Get(context);

            string passphraseString = null;
            if (SignData)
            {
                passphraseString = Passphrase.Get(context);
                if (string.IsNullOrWhiteSpace(passphraseString))
                {
                    var secure = PassphraseSecureString.Get(context);
                    if (secure == null || secure.Length == 0)
                        throw new ArgumentNullException(nameof(Passphrase), Resources.Activity_EncryptText_Property_Passphrase_Name);
                    passphraseString = new NetworkCredential(string.Empty, secure).Password;
                }
            }

            return PgpStreamHelper.WithPgpEncryptStreams(
                publicKeyFilePath, privateKeyFilePath, passphraseString, SignData,
                (pubStream, privStream, pass) =>
                    CryptographyHelper.PgpEncryptText(input, pubStream, privStream, pass, SignData));
        }

        private string ExecuteSymmetricEncrypt(CodeActivityContext context, string input)
        {
            var key = Key.Get(context);
            var keySecureString = KeySecureString.Get(context);
            var keyEncoding = Encoding.Get(context);
            var keyEncodingString = KeyEncodingString.Get(context);
            var ivString = Iv?.Get(context);
            var iterations = KdfIterations?.Get(context) ?? 0;

            SymmetricInteropHelper.ValidateInteropSettings(Algorithm, Format, KeyFormat, ivString, iterations, null);

            if (string.IsNullOrWhiteSpace(key))
            {
                if (keySecureString == null || keySecureString.Length == 0)
                    throw new ArgumentNullException(nameof(Key), Resources.Activity_EncryptText_Property_Key_Name);
                key = null;
            }
            if (keyEncoding == null && string.IsNullOrEmpty(keyEncodingString))
                throw new ArgumentNullException(Resources.Encoding);

            keyEncoding = EncodingHelpers.KeyEncodingOrString(keyEncoding, keyEncodingString);

            byte[] keyOrPasswordBytes = SymmetricInteropHelper.ParseKeyOrIv(key, keySecureString, KeyFormat, keyEncoding);
            byte[] ivBytes = SymmetricInteropHelper.ParseKeyOrIv(ivString, null, KeyFormat, keyEncoding);

            if (Format == SymmetricWireFormat.Raw)
                SymmetricInteropHelper.ValidateInteropSettings(Algorithm, Format, KeyFormat, ivString, iterations, keyOrPasswordBytes?.Length);

            try
            {
                byte[] encrypted = SymmetricInteropHelper.DispatchEncrypt(
                    Algorithm, Format, iterations, keyOrPasswordBytes, ivBytes, keyEncoding.GetBytes(input));

                return Convert.ToBase64String(encrypted);
            }
            finally
            {
                SymmetricInteropHelper.ClearKeyBytes(keyOrPasswordBytes);
            }
        }
    }
}

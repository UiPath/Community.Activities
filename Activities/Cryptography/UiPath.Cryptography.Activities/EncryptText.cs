using System;
using System.Activities;
using System.Activities.Expressions;
using System.Activities.Validation;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Security;
using System.Text;
using UiPath.Cryptography.Activities.Helpers;
using UiPath.Cryptography.Activities.Properties;
using UiPath.Cryptography.Enums;
using UiPath.Shared.Activities;
#if ENABLE_DEFAULT_TELEMETRY
using UiPath.Shared.Telemetry.Services;
#endif

#pragma warning disable CS0618 // obsolete encryption algorithm

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
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_EncryptText_Property_KeyInputModeSwitch_Name))]
        [LocalizedDescription(nameof(Resources.Activity_EncryptText_Property_KeyInputModeSwitch_Description))]
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

        [LocalizedCategory(nameof(Resources.Output))]
        [LocalizedDisplayName(nameof(Resources.Activity_EncryptText_Property_Result_Name))]
        [LocalizedDescription(nameof(Resources.Activity_EncryptText_Property_Result_Description))]
        public new OutArgument<string> Result { get => base.Result; set => base.Result = value; }

        [DefaultValue(null)]
        [LocalizedCategory(nameof(Resources.Common))]
        [LocalizedDisplayName(nameof(Resources.Activity_EncryptText_Property_ContinueOnError_Name))]
        [LocalizedDescription(nameof(Resources.Activity_EncryptText_Property_ContinueOnError_Description))]
        public InArgument<bool> ContinueOnError { get; set; }

        [DefaultValue(null)]
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_EncryptText_Property_PublicKeyFilePath_Name))]
        [LocalizedDescription(nameof(Resources.Activity_EncryptText_Property_PublicKeyFilePath_Description))]
        public InArgument<string> PublicKeyFilePath { get; set; }

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
        public InArgument<SecureString> Passphrase { get; set; }

        public EncryptText()
        {
            Algorithm = EncryptionAlgorithm.AESGCM;
            KeyEncodingString = System.Text.Encoding.UTF8.CodePage.ToString();
        }

        protected override void CacheMetadata(CodeActivityMetadata metadata)
        {
            base.CacheMetadata(metadata);

            if (Algorithm == EncryptionAlgorithm.PGP)
            {
                return;
            }

            if (!CryptographyHelper.IsFipsCompliant(Algorithm))
            {
                var error = new ValidationError(Resources.FipsComplianceWarning, true, nameof(Algorithm));
                metadata.AddValidationError(error);
            }

            if (Key == null && KeyInputModeSwitch == KeyInputMode.Key)
            {
                var error = new ValidationError(Resources.KeyNullError, false, nameof(Key));
                metadata.AddValidationError(error);
            }
            if (KeySecureString == null && KeyInputModeSwitch == KeyInputMode.SecureKey)
            {
                var error = new ValidationError(Resources.KeySecureStringNullError, false, nameof(KeySecureString));
                metadata.AddValidationError(error);
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
            var privateKeyFilePath = PrivateKeyFilePath.Get(context);
            var passphrase = Passphrase.Get(context);

            return PgpStreamHelper.WithPgpEncryptStreams(
                publicKeyFilePath, privateKeyFilePath, passphrase, SignData,
                (pubStream, privStream, pass) =>
                    CryptographyHelper.PgpEncryptText(input, pubStream, privStream, pass, SignData));
        }

        private string ExecuteSymmetricEncrypt(CodeActivityContext context, string input)
        {
            var key = Key.Get(context);
            var keySecureString = KeySecureString.Get(context);
            var keyEncoding = Encoding.Get(context);
            var keyEncodingString = KeyEncodingString.Get(context);

            if (string.IsNullOrWhiteSpace(key) && KeyInputModeSwitch == KeyInputMode.Key)
                throw new ArgumentNullException(Resources.Activity_KeyedHashText_Property_Key_Name);
            if ((keySecureString == null || keySecureString?.Length == 0) && KeyInputModeSwitch == KeyInputMode.SecureKey)
                throw new ArgumentNullException(Resources.Activity_KeyedHashText_Property_KeySecureString_Name);
            if (keyEncoding == null && string.IsNullOrEmpty(keyEncodingString))
                throw new ArgumentNullException(Resources.Encoding);

            keyEncoding = EncodingHelpers.KeyEncodingOrString(keyEncoding, keyEncodingString);

            var encrypted = CryptographyHelper.EncryptData(Algorithm, keyEncoding.GetBytes(input), CryptographyHelper.KeyEncoding(keyEncoding, key, keySecureString));

            return Convert.ToBase64String(encrypted);
        }
    }
}
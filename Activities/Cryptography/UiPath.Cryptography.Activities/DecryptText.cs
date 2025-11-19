using System;
using System.Activities;
using System.Activities.Expressions;
using System.Activities.Validation;
using System.ComponentModel;
using System.Diagnostics;
using System.Security;
using System.Security.Cryptography;
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
    [LocalizedDisplayName(nameof(Resources.Activity_DecryptText_Name))]
    [LocalizedDescription(nameof(Resources.Activity_DecryptText_Description))]
    public partial class DecryptText : CodeActivity<string>
    {
        [RequiredArgument]
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_DecryptText_Property_Algorithm_Name))]
        [LocalizedDescription(nameof(Resources.Activity_DecryptText_Property_Algorithm_Description))]
        public SymmetricAlgorithms Algorithm { get; set; }

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
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_DecryptText_Property_KeyInputModeSwitch_Name))]
        [LocalizedDescription(nameof(Resources.Activity_DecryptText_Property_KeyInputModeSwitch_Description))]
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
        [LocalizedCategory(nameof(Resources.Common))]
        [LocalizedDisplayName(nameof(Resources.Activity_DecryptText_Property_ContinueOnError_Name))]
        [LocalizedDescription(nameof(Resources.Activity_DecryptText_Property_Result_Description))]
        public InArgument<bool> ContinueOnError { get; set; }

        public DecryptText()
        {
            Algorithm = SymmetricAlgorithms.AESGCM;
            KeyEncodingString = System.Text.Encoding.UTF8.CodePage.ToString();

        }

        protected override void CacheMetadata(CodeActivityMetadata metadata)
        {
            base.CacheMetadata(metadata);

            switch (Algorithm)
            {
                case SymmetricAlgorithms.RC2:
                case SymmetricAlgorithms.Rijndael:
                    var error = new ValidationError(Resources.FipsComplianceWarning, true, nameof(Algorithm));
                    metadata.AddValidationError(error);
                    break;

                default:
                    break;
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
            ITelemetryOperationWrapper telemetryOperation = null;
#if ENABLE_DEFAULT_TELEMETRY
            telemetryOperation = RuntimeTelemetryService.CreateExecutionOperation(this, context);
#endif
            
            string result = null;
            try
            {
                var input = Input.Get(context);
                var key = Key.Get(context);
                var keySecureString = KeySecureString.Get(context);
                var keyEncoding = Encoding.Get(context);
                var keyEncodingString = KeyEncodingString.Get(context);

                if (string.IsNullOrWhiteSpace(input))
                    throw new ArgumentNullException(Resources.InputStringDisplayName);

                if (string.IsNullOrWhiteSpace(key) && KeyInputModeSwitch == KeyInputMode.Key)
                {
                    throw new ArgumentNullException(Resources.Activity_KeyedHashText_Property_Key_Name);
                }
                if ((keySecureString == null || keySecureString?.Length == 0) && KeyInputModeSwitch == KeyInputMode.SecureKey)
                {
                    throw new ArgumentNullException(Resources.Activity_KeyedHashText_Property_KeySecureString_Name);
                }

                if (keyEncoding == null && string.IsNullOrEmpty(keyEncodingString))
                    throw new ArgumentNullException(Resources.Encoding);

                keyEncoding = EncodingHelpers.KeyEncodingOrString(keyEncoding, keyEncodingString);

                byte[] decrypted = null;
                try
                {
                    decrypted = CryptographyHelper.DecryptData(Algorithm, Convert.FromBase64String(input), CryptographyHelper.KeyEncoding(keyEncoding, key, keySecureString));
                }
                catch (CryptographicException ex)
                {
                    throw new InvalidOperationException(Resources.GenericCryptographicException, ex);
                }

                result = keyEncoding.GetString(decrypted);
                telemetryOperation?.Send();
            }
            catch (Exception ex)
            {
                telemetryOperation?.SendWithException(ex);
                Trace.TraceError(ex.ToString());

                if (!ContinueOnError.Get(context))
                {
                    throw;
                }
            }
            return result;
        }
    }
}
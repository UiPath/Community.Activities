using System;
using System.Activities;
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

#pragma warning disable CS0618 // CryptographyHelper is intentionally marked Obsolete to discourage external use; in-package consumers are expected.

namespace UiPath.Cryptography.Activities
{
    [LocalizedDisplayName(nameof(Resources.Activity_KeyedHashText_Name))]
    [LocalizedDescription(nameof(Resources.Activity_KeyedHashText_Description))]
    public partial class KeyedHashText : CodeActivity<string>
    {
        [RequiredArgument]
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_KeyedHashText_Property_Algorithm_Name))]
        [LocalizedDescription(nameof(Resources.Activity_KeyedHashText_Property_Algorithm_Description))]
        public KeyedHashAlgorithms Algorithm { get; set; }

        [RequiredArgument]
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_KeyedHashText_Property_Input_Name))]
        [LocalizedDescription(nameof(Resources.Activity_KeyedHashText_Property_Input_Description))]
        public InArgument<string> Input { get; set; }

        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_KeyedHashText_Property_Key_Name))]
        [LocalizedDescription(nameof(Resources.Activity_KeyedHashText_Property_Key_Description))]
        public InArgument<string> Key { get; set; }

        [Browsable(false)]
        [Obsolete("Legacy property kept for XAML back-compat with workflows that persisted the active key input mode. The activity now infers the mode from which side is bound.")]
        public KeyInputMode KeyInputModeSwitch { get; set; }

        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_KeyedHashText_Property_KeySecureString_Name))]
        [LocalizedDescription(nameof(Resources.Activity_KeyedHashText_Property_KeySecureString_Description))]
        public InArgument<SecureString> KeySecureString { get; set; }

        [Browsable(false)]
        [LocalizedCategory(nameof(Resources.Category_Encoding_Name))]
        [LocalizedDisplayName(nameof(Resources.Activity_KeyedHashText_Property_Encoding_Name))]
        [LocalizedDescription(nameof(Resources.Activity_KeyedHashText_Property_Encoding_Description))]
        public InArgument<Encoding> Encoding { get; set; }

        [Browsable(false)]
        public InArgument<string> KeyEncodingString { get; set; }

        [LocalizedCategory(nameof(Resources.Output))]
        [LocalizedDisplayName(nameof(Resources.Activity_KeyedHashText_Property_Result_Name))]
        [LocalizedDescription(nameof(Resources.Activity_KeyedHashText_Property_Result_Description))]
        public new OutArgument<string> Result { get => base.Result; set => base.Result = value; }

        [DefaultValue(null)]
        [LocalizedCategory(nameof(Resources.Category_Options_Name))]
        [LocalizedDisplayName(nameof(Resources.Activity_KeyedHashText_Property_ContinueOnError_Name))]
        [LocalizedDescription(nameof(Resources.Activity_KeyedHashText_Property_ContinueOnError_Description))]
        public InArgument<bool> ContinueOnError { get; set; }

        public KeyedHashText()
        {
            Algorithm = KeyedHashAlgorithms.HMACSHA256;
            KeyEncodingString = System.Text.Encoding.UTF8.CodePage.ToString();
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

                if (Algorithm.ToString().StartsWith(nameof(HMAC)))
                {
                    if (string.IsNullOrWhiteSpace(key))
                    {
                        if (keySecureString == null || keySecureString.Length == 0)
                            throw new ArgumentNullException(nameof(Key), Resources.Activity_KeyedHashText_Property_Key_Name);
                        key = null; // ensure helper falls back to SecureString
                    }
                }

                if (keyEncoding == null && string.IsNullOrEmpty(keyEncodingString)) throw new ArgumentNullException(Resources.Encoding);

                keyEncoding = EncodingHelpers.KeyEncodingOrString(keyEncoding, keyEncodingString);

                var hashed = CryptographyHelper.HashDataWithKey(Algorithm, keyEncoding.GetBytes(input), CryptographyHelper.KeyEncoding(keyEncoding, key, keySecureString));

                telemetryOperation?.Send();
                result = BitConverter.ToString(hashed).Replace("-", string.Empty);
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

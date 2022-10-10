using System;
using System.Activities;
using System.Activities.Expressions;
using System.Activities.Validation;
using System.ComponentModel;
using System.Diagnostics;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using UiPath.Cryptography.Activities.Properties;
using UiPath.Cryptography.Enums;
using UiPath.Studio.Activities.Api.Activities;

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
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_KeyedHashText_Property_Key_Name))]
        [LocalizedDescription(nameof(Resources.Activity_KeyedHashText_Property_Key_Description))]
        public KeyInputMode KeyInputModeSwitch { get; set; }

        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_KeyedHashText_Property_KeySecureString_Name))]
        [LocalizedDescription(nameof(Resources.Activity_KeyedHashText_Property_KeySecureString_Description))]
        public InArgument<SecureString> KeySecureString { get; set; }

        [Browsable(false)]
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_KeyedHashText_Property_Encoding_Name))]
        [LocalizedDescription(nameof(Resources.Activity_KeyedHashText_Property_Encoding_Description))]
        public InArgument<Encoding> Encoding { get; set; }

        [LocalizedCategory(nameof(Resources.Output))]
        [LocalizedDisplayName(nameof(Resources.Activity_KeyedHashText_Property_Result_Name))]
        [LocalizedDescription(nameof(Resources.Activity_KeyedHashText_Property_Result_Description))]
        public new OutArgument<string> Result { get => base.Result; set => base.Result = value; }

        [DefaultValue(null)]
        [LocalizedCategory(nameof(Resources.Common))]
        [LocalizedDisplayName(nameof(Resources.Activity_KeyedHashText_Property_ContinueOnError_Name))]
        [LocalizedDescription(nameof(Resources.Activity_KeyedHashText_Property_ContinueOnError_Description))]
        public InArgument<bool> ContinueOnError { get; set; }

        public KeyedHashText()
        {
            Algorithm = KeyedHashAlgorithms.HMACSHA256;
            Encoding = new InArgument<Encoding>(ExpressionServices.Convert((env) => System.Text.Encoding.UTF8));
        }

        protected override void CacheMetadata(CodeActivityMetadata metadata)
        {
            base.CacheMetadata(metadata);

            if (!CryptographyHelper.IsFipsCompliant(Algorithm))
            {
                var error = new ValidationError(Resources.FipsComplianceWarning, true, nameof(Algorithm));
                metadata.AddValidationError(error);
            }
            if (Algorithm.ToString().StartsWith(nameof(HMAC)))
            {
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
#if NET461
            if (Algorithm == KeyedHashAlgorithms.MACTripleDES)
            {
                var keySizeWarning = new ValidationError(Resources.MacTripleDesKeySizeWarning, true, nameof(Algorithm));
                metadata.AddValidationError(keySizeWarning);
            }
#endif
        }

        protected override string Execute(CodeActivityContext context)
        {
            string result = null;

            try
            {
                var input = Input.Get(context);
                var key = Key.Get(context);
                var keySecureString = KeySecureString.Get(context);
                var keyEncoding = Encoding.Get(context);

                if (string.IsNullOrWhiteSpace(input))
                    throw new ArgumentNullException(Resources.InputStringDisplayName);

                if (Algorithm.ToString().StartsWith(nameof(HMAC)))
                {
                    if (string.IsNullOrWhiteSpace(key) && KeyInputModeSwitch == KeyInputMode.Key)
                    {
                        throw new ArgumentNullException(Resources.Activity_KeyedHashText_Property_Key_Name);
                    }
                    if ((keySecureString == null || keySecureString?.Length == 0) && KeyInputModeSwitch == KeyInputMode.SecureKey)
                    {
                        throw new ArgumentNullException(Resources.Activity_KeyedHashText_Property_KeySecureString_Name);
                    }
                }

                if (keyEncoding == null)
                    throw new ArgumentNullException(Resources.Encoding);

                var hashed = CryptographyHelper.HashDataWithKey(Algorithm, keyEncoding.GetBytes(input), CryptographyHelper.KeyEncoding(keyEncoding, key, keySecureString));

                result = BitConverter.ToString(hashed).Replace("-", string.Empty);
            }
            catch (Exception ex)
            {
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
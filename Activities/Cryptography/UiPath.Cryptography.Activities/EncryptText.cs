using System;
using System.Activities;
using System.Activities.Expressions;
using System.Activities.Validation;
using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Security;
using System.Text;
using UiPath.Cryptography.Activities.Properties;

namespace UiPath.Cryptography.Activities
{
    [LocalizedDisplayName(nameof(Resources.EncryptTextDisplayName))]
    [LocalizedDescription(nameof(Resources.EncryptTextDescription))]
    public class EncryptText : CodeActivity<string>
    {
        [RequiredArgument]
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.AlgorithmDisplayName))]
        [LocalizedDescription(nameof(Resources.EncryptAlgorithmDescription))]
        public SymmetricAlgorithms Algorithm { get; set; }

        [RequiredArgument]
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.InputStringDisplayName))]
        [LocalizedDescription(nameof(Resources.EncryptTextInputDescription))]
        public InArgument<string> Input { get; set; }

        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.KeyDisplayName))]
        [LocalizedDescription(nameof(Resources.EncryptTextKeyDescription))]
        public InArgument<string> Key { get; set; }

        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.KeySecureStringDisplayName))]
        [LocalizedDescription(nameof(Resources.EncryptTextKeySecureStringDescription))]
        public InArgument<SecureString> KeySecureString { get; set; }

        [RequiredArgument]
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.EncodingDisplayName))]
        [LocalizedDescription(nameof(Resources.EncryptTextEncodingDescription))]
        public InArgument<Encoding> Encoding { get; set; }

        [RequiredArgument]
        [LocalizedCategory(nameof(Resources.Output))]
        [LocalizedDisplayName(nameof(Resources.ResultDisplayName))]
        [LocalizedDescription(nameof(Resources.EncryptTextResultDescription))]
        public new OutArgument<string> Result { get => base.Result; set => base.Result = value; }

        [DefaultValue(null)]
        [LocalizedCategory(nameof(Resources.Common))]
        [LocalizedDisplayName(nameof(Resources.ContinueOnErrorDisplayName))]
        public InArgument<bool> ContinueOnError { get; set; }

        public EncryptText()
        {
            Algorithm = SymmetricAlgorithms.AESGCM;
            Encoding = new InArgument<Encoding>(ExpressionServices.Convert((env) => System.Text.Encoding.UTF8));
        }

        protected override void CacheMetadata(CodeActivityMetadata metadata)
        {
            base.CacheMetadata(metadata);

            switch (Algorithm)
            {
                case SymmetricAlgorithms.RC2:
                case SymmetricAlgorithms.Rijndael:
                    ValidationError error = new ValidationError(Resources.FipsComplianceWarning, true, nameof(Algorithm));
                    metadata.AddValidationError(error);
                    break;

                default:
                    break;
            }
        }

        protected override string Execute(CodeActivityContext context)
        {
            string result = null;

            try
            {
                string input = Input.Get(context);
                string key = Key.Get(context);
                SecureString keySecureString = KeySecureString.Get(context);
                Encoding keyEncoding = Encoding.Get(context);

                if (string.IsNullOrWhiteSpace(input))
                {
                    throw new ArgumentNullException(Resources.InputStringDisplayName);
                }
                if (string.IsNullOrWhiteSpace(key) && keySecureString == null)
                {
                    throw new ArgumentNullException(Resources.KeyAndSecureStringNull);
                }
                if (key != null && keySecureString != null)
                {
                    throw new ArgumentNullException(Resources.KeyAndSecureStringNotNull);
                }
                if (keyEncoding == null)
                {
                    throw new ArgumentNullException(Resources.Encoding);
                }

                byte[] encrypted = CryptographyHelper.EncryptData(Algorithm, keyEncoding.GetBytes(input), CryptographyHelper.KeyEncoding(keyEncoding, key, keySecureString));

                result = Convert.ToBase64String(encrypted);
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
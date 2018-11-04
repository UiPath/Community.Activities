using Microsoft.VisualBasic.Activities;
using System;
using System.Activities;
using System.Activities.Validation;
using System.ComponentModel;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using UiPath.Cryptography.Activities.Properties;

namespace UiPath.Cryptography.Activities
{
    [LocalizedDisplayName(nameof(Resources.DecryptTextDisplayName))]
    [LocalizedDescription(nameof(Resources.DecryptTextDescription))]
    public class DecryptText : CodeActivity<string>
    {
        [RequiredArgument]
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.AlgorithmDisplayName))]
        [LocalizedDescription(nameof(Resources.DecryptAlgorithmDescription))]
        public SymmetricAlgorithms Algorithm { get; set; }

        [RequiredArgument]
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.InputStringDisplayName))]
        [LocalizedDescription(nameof(Resources.DecryptTextInputDescription))]
        public InArgument<string> Input { get; set; }

        [RequiredArgument]
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.KeyDisplayName))]
        [LocalizedDescription(nameof(Resources.DecryptTextKeyDescription))]
        public InArgument<string> Key { get; set; }

        [RequiredArgument]
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.EncodingDisplayName))]
        [LocalizedDescription(nameof(Resources.DecryptTextEncodingDescription))]
        public InArgument<Encoding> Encoding { get; set; }

        [RequiredArgument]
        [LocalizedCategory(nameof(Resources.Output))]
        [LocalizedDisplayName(nameof(Resources.ResultDisplayName))]
        [LocalizedDescription(nameof(Resources.DecryptTextResultDescription))]
        public new OutArgument<string> Result { get => base.Result; set => base.Result = value; }

        [DefaultValue(null)]
        [LocalizedCategory(nameof(Resources.Common))]
        [LocalizedDisplayName(nameof(Resources.ContinueOnErrorDisplayName))]
        public InArgument<bool> ContinueOnError { get; set; }

        public DecryptText()
        {
            Algorithm = SymmetricAlgorithms.AES;
            Encoding = new VisualBasicValue<Encoding>(typeof(Encoding).FullName + "." + nameof(System.Text.Encoding.UTF8)); // Kinda ugly.
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
                Encoding encoding = Encoding.Get(context);

                if (string.IsNullOrWhiteSpace(input))
                {
                    throw new ArgumentNullException(Resources.InputStringDisplayName);
                }
                if (string.IsNullOrWhiteSpace(key))
                {
                    throw new ArgumentNullException(Resources.Key);
                }
                if (encoding == null)
                {
                    throw new ArgumentNullException(Resources.Encoding);
                }

                byte[] decrypted = null;
                try
                {
                    decrypted = CryptographyHelper.DecryptData(Algorithm, Convert.FromBase64String(input), encoding.GetBytes(key));
                }
                catch (CryptographicException ex)
                {
                    throw new InvalidOperationException(Resources.GenericCryptographicException, ex);
                }

                result = encoding.GetString(decrypted);
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

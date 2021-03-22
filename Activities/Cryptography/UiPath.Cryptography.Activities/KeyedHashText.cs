using Microsoft.VisualBasic.Activities;
using System;
using System.Activities;
using System.Activities.Validation;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using UiPath.Cryptography.Activities.Properties;

namespace UiPath.Cryptography.Activities
{
    [LocalizedDisplayName(nameof(Resources.KeyedHashTextDisplayName))]
    [LocalizedDescription(nameof(Resources.KeyedHashTextDescription))]
    public class KeyedHashText : CodeActivity<string>
    {
        [RequiredArgument]
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.AlgorithmDisplayName))]
        [LocalizedDescription(nameof(Resources.KeyedHashAlgorithmDescription))]
        public KeyedHashAlgorithms Algorithm { get; set; }

        [RequiredArgument]
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.InputStringDisplayName))]
        [LocalizedDescription(nameof(Resources.HashTextInputDescription))]
        public InArgument<string> Input { get; set; }

        [RequiredArgument]
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.KeyDisplayName))]
        [LocalizedDescription(nameof(Resources.KeyedHashTextKeyDescription))]
        public InArgument<string> Key { get; set; }

        [RequiredArgument]
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.EncodingDisplayName))]
        [LocalizedDescription(nameof(Resources.KeyedHashEncodingDescription))]
        public InArgument<Encoding> Encoding { get; set; }

        [RequiredArgument]
        [LocalizedCategory(nameof(Resources.Output))]
        [LocalizedDisplayName(nameof(Resources.ResultDisplayName))]
        [LocalizedDescription(nameof(Resources.HashTextResultDescription))]
        public new OutArgument<string> Result { get => base.Result; set => base.Result = value; }

        [DefaultValue(null)]
        [LocalizedCategory(nameof(Resources.Common))]
        [LocalizedDisplayName(nameof(Resources.ContinueOnErrorDisplayName))]
        public InArgument<bool> ContinueOnError { get; set; }

        public KeyedHashText()
        {
            Algorithm = KeyedHashAlgorithms.HMACSHA256;
            Encoding = new VisualBasicValue<Encoding>(typeof(Encoding).FullName + "." + nameof(System.Text.Encoding.UTF8)); // Kinda ugly.
        }

        protected override void CacheMetadata(CodeActivityMetadata metadata)
        {
            base.CacheMetadata(metadata);

            if (!CryptographyHelper.IsFipsCompliant(Algorithm))
            {
                ValidationError error = new ValidationError(Resources.FipsComplianceWarning, true, nameof(Algorithm));
                metadata.AddValidationError(error);
            }
#if NET461
            if (Algorithm == KeyedHashAlgorithms.MACTripleDES)
            {
                ValidationError keySizeWarning = new ValidationError(Resources.MacTripleDesKeySizeWarning, true, nameof(Algorithm));
                metadata.AddValidationError(keySizeWarning);
            }
#endif
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

                byte[] hashed = CryptographyHelper.HashDataWithKey(Algorithm, encoding.GetBytes(input), encoding.GetBytes(key));

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
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
#if NET461
    [LocalizedDisplayName(nameof(Resources.HashTextDisplayName))]
    [LocalizedDescription(nameof(Resources.HashTextDescription))]
    public class HashText : CodeActivity<string>
    {
        [RequiredArgument]
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.AlgorithmDisplayName))]
        [LocalizedDescription(nameof(Resources.HashAlgorithmDescription))]
        public HashAlgorithms Algorithm { get; set; }

        [RequiredArgument]
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.InputStringDisplayName))]
        [LocalizedDescription(nameof(Resources.HashTextInputDescription))]
        public InArgument<string> Input { get; set; }

        [RequiredArgument]
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.EncodingDisplayName))]
        [LocalizedDescription(nameof(Resources.EncodingDescription))]
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

        public HashText()
        {

            Algorithm = HashAlgorithms.SHA256;
            Encoding = new VisualBasicValue<Encoding>(typeof(Encoding).FullName + "." + nameof(System.Text.Encoding.UTF8)); // Kinda ugly.
        }

        protected override void CacheMetadata(CodeActivityMetadata metadata)
        {
            base.CacheMetadata(metadata);

            switch (Algorithm)
            {
                case HashAlgorithms.MD5:
                case HashAlgorithms.RIPEMD160:
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
                Encoding encoding = Encoding.Get(context);

                if (string.IsNullOrWhiteSpace(input))
                {
                    throw new ArgumentNullException(Resources.InputStringDisplayName);
                }
                if (encoding == null)
                {
                    throw new ArgumentNullException(Resources.Encoding);
                }

                byte[] hashed = CryptographyHelper.HashData(Algorithm, encoding.GetBytes(input));

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
#endif
}
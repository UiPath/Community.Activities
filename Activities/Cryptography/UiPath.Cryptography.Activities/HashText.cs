using System;
using System.Activities;
using System.Activities.Expressions;
using System.Activities.Validation;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using UiPath.Cryptography.Activities.Properties;

namespace UiPath.Cryptography.Activities
{
#if NET461
    [LocalizedDisplayName(nameof(Resources.Activity_HashText_Name))]
    [LocalizedDescription(nameof(Resources.Activity_HashText_Description))]
    public class HashText : CodeActivity<string>
    {
        [RequiredArgument]
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_HashText_Property_Algorithm_Name))]
        [LocalizedDescription(nameof(Resources.Activity_HashText_Property_Algorithm_Description))]
        public HashAlgorithms Algorithm { get; set; }

        [RequiredArgument]
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_HashText_Property_Input_Name))]
        [LocalizedDescription(nameof(Resources.Activity_HashText_Property_Input_Description))]
        public InArgument<string> Input { get; set; }

        [RequiredArgument]
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_HashText_Property_Encoding_Name))]
        [LocalizedDescription(nameof(Resources.Activity_HashText_Property_Encoding_Description))]
        public InArgument<Encoding> Encoding { get; set; }

        [LocalizedCategory(nameof(Resources.Output))]
        [LocalizedDisplayName(nameof(Resources.Activity_HashText_Property_Result_Name))]
        [LocalizedDescription(nameof(Resources.Activity_HashText_Property_Result_Description))]
        public new OutArgument<string> Result { get => base.Result; set => base.Result = value; }

        [DefaultValue(null)]
        [LocalizedCategory(nameof(Resources.Common))]
        [LocalizedDisplayName(nameof(Resources.Activity_HashText_Property_ContinueOnError_Name))]
        [LocalizedDescription(nameof(Resources.Activity_HashText_Property_ContinueOnError_Description))]
        public InArgument<bool> ContinueOnError { get; set; }

        public HashText()
        {

            Algorithm = HashAlgorithms.SHA256;
            Encoding = new InArgument<Encoding>(ExpressionServices.Convert((env) => System.Text.Encoding.UTF8));
        }

        protected override void CacheMetadata(CodeActivityMetadata metadata)
        {
            base.CacheMetadata(metadata);

            switch (Algorithm)
            {
                case HashAlgorithms.MD5:
                case HashAlgorithms.RIPEMD160:
                    var error = new ValidationError(Resources.FipsComplianceWarning, true, nameof(Algorithm));
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
                var input = Input.Get(context);
                var encoding = Encoding.Get(context);

                if (string.IsNullOrWhiteSpace(input))
                    throw new ArgumentNullException(Resources.InputStringDisplayName);

                if (encoding == null)
                    throw new ArgumentNullException(Resources.Encoding);

                var hashed = CryptographyHelper.HashData(Algorithm, encoding.GetBytes(input));

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
#if NET
    [Browsable(false)]
    [LocalizedDisplayName(nameof(Resources.Activity_HashText_Name))]
    [LocalizedDescription(nameof(Resources.Activity_HashText_Description))]
    public partial class HashText : KeyedHashText
    {
    }
#endif
}
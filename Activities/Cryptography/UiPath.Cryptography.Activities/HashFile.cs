using System;
using System.Activities;
using System.Activities.Validation;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using UiPath.Cryptography.Activities.Properties;

namespace UiPath.Cryptography.Activities
{
#if NET461
    [LocalizedDisplayName(nameof(Resources.Activity_HashFile_Name))]
    [LocalizedDescription(nameof(Resources.Activity_HashFile_Description))]
    public class HashFile : CodeActivity<string>
    {
        [RequiredArgument]
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_HashFile_Property_Algorithm_Name))]
        [LocalizedDescription(nameof(Resources.Activity_HashFile_Property_Algorithm_Description))]
        public HashAlgorithms Algorithm { get; set; }

        [RequiredArgument]
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_HashFile_Property_FilePath_Name))]
        [LocalizedDescription(nameof(Resources.Activity_HashFile_Property_FilePath_Description))]
        public InArgument<string> FilePath { get; set; }

        [LocalizedCategory(nameof(Resources.Output))]
        [LocalizedDisplayName(nameof(Resources.Activity_HashFile_Property_Result_Name))]
        [LocalizedDescription(nameof(Resources.Activity_HashFile_Property_Result_Description))]
        public new OutArgument<string> Result { get => base.Result; set => base.Result = value; }

        [DefaultValue(null)]
        [LocalizedCategory(nameof(Resources.Common))]
        [LocalizedDisplayName(nameof(Resources.Activity_HashFile_Property_ContinueOnError_Name))]
        [LocalizedDescription(nameof(Resources.Activity_HashFile_Property_ContinueOnError_Description))]
        public InArgument<bool> ContinueOnError { get; set; }

        public HashFile()
        {
            Algorithm = HashAlgorithms.SHA256;
        }

        protected override void CacheMetadata(CodeActivityMetadata metadata)
        {
            base.CacheMetadata(metadata);

            if (!CryptographyHelper.IsFipsCompliant(Algorithm))
            {
                var error = new ValidationError(Resources.FipsComplianceWarning, true, nameof(Algorithm));
                metadata.AddValidationError(error);
            }
        }

        protected override string Execute(CodeActivityContext context)
        {
            string result = null;

            try
            {
                var filePath = FilePath.Get(context);

                if (string.IsNullOrWhiteSpace(filePath))
                    throw new ArgumentNullException(Resources.FilePathDisplayName);

                if (!File.Exists(filePath))
                    throw new ArgumentException(Resources.FileDoesNotExistsException, Resources.FilePathDisplayName);

                var hashed = CryptographyHelper.HashData(Algorithm, File.ReadAllBytes(filePath));

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
    [LocalizedDisplayName(nameof(Resources.Activity_HashFile_Name))]
    [LocalizedDescription(nameof(Resources.Activity_HashFile_Description))]
    public partial class HashFile : KeyedHashFile
    {
    }
#endif
}
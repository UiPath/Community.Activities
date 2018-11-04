using System;
using System.Activities;
using System.Activities.Validation;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using UiPath.Cryptography.Activities.Properties;

namespace UiPath.Cryptography.Activities
{
    [LocalizedDisplayName(nameof(Resources.HashFileDisplayName))]
    [LocalizedDescription(nameof(Resources.HashFileDescription))]
    public class HashFile : CodeActivity<string>
    {
        [RequiredArgument]
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.AlgorithmDisplayName))]
        [LocalizedDescription(nameof(Resources.HashAlgorithmDescription))]
        public HashAlgorithms Algorithm { get; set; }

        [RequiredArgument]
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.FilePathDisplayName))]
        [LocalizedDescription(nameof(Resources.HashFilePathDescription))]
        public InArgument<string> FilePath { get; set; }

        [RequiredArgument]
        [LocalizedCategory(nameof(Resources.Output))]
        [LocalizedDisplayName(nameof(Resources.ResultDisplayName))]
        [LocalizedDescription(nameof(Resources.HashFileResultDescription))]
        public new OutArgument<string> Result { get => base.Result; set => base.Result = value; }

        [DefaultValue(null)]
        [LocalizedCategory(nameof(Resources.Common))]
        [LocalizedDisplayName(nameof(Resources.ContinueOnErrorDisplayName))]
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
                ValidationError error = new ValidationError(Resources.FipsComplianceWarning, true, nameof(Algorithm));
                metadata.AddValidationError(error);
            }
        }

        protected override string Execute(CodeActivityContext context)
        {
            string result = null;

            try
            {
                string filePath = FilePath.Get(context);

                if (string.IsNullOrWhiteSpace(filePath))
                {
                    throw new ArgumentNullException(Resources.FilePathDisplayName);
                }
                if (!File.Exists(filePath))
                {
                    throw new ArgumentException(Resources.FileDoesNotExistsException, Resources.FilePathDisplayName);
                }

                byte[] hashed = CryptographyHelper.HashData(Algorithm, File.ReadAllBytes(filePath));

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

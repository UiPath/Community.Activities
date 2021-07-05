using Microsoft.VisualBasic.Activities;
using System;
using System.Activities;
using System.Activities.Validation;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using UiPath.Cryptography.Activities.Properties;

namespace UiPath.Cryptography.Activities
{
    [LocalizedDisplayName(nameof(Resources.DecryptFileDisplayName))]
    [LocalizedDescription(nameof(Resources.DecryptFileDescription))]
    public class DecryptFile : CodeActivity
    {
        [RequiredArgument]
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.AlgorithmDisplayName))]
        [LocalizedDescription(nameof(Resources.DecryptAlgorithmDescription))]
        public SymmetricAlgorithms Algorithm { get; set; }

        [RequiredArgument]
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.InputFilePathDisplayName))]
        [LocalizedDescription(nameof(Resources.DecryptFileInputPathDescription))]
        public InArgument<string> InputFilePath { get; set; }

        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.KeyDisplayName))]
        [LocalizedDescription(nameof(Resources.DecryptFileKeyDescription))]
        public InArgument<string> Key { get; set; }

        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.KeySecureStringDisplayName))]
        [LocalizedDescription(nameof(Resources.DecryptFileKeySecureStringDescription))]
        public InArgument<SecureString> KeySecureString { get; set; }

        [RequiredArgument]
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.KeyEncodingDisplayName))]
        [LocalizedDescription(nameof(Resources.DecryptFileEncodingDescription))]
        public InArgument<Encoding> KeyEncoding { get; set; }

        [RequiredArgument]
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.OutputFilePathDisplayName))]
        [LocalizedDescription(nameof(Resources.DecryptFileOutputPathDescription))]
        public InArgument<string> OutputFilePath { get; set; }

        [RequiredArgument]
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.OverwriteDisplayName))]
        [LocalizedDescription(nameof(Resources.OverwriteDescription))]
        public bool Overwrite { get; set; }

        [DefaultValue(null)]
        [LocalizedCategory(nameof(Resources.Common))]
        [LocalizedDisplayName(nameof(Resources.ContinueOnErrorDisplayName))]
        public InArgument<bool> ContinueOnError { get; set; }

        public DecryptFile()
        {
            Algorithm = SymmetricAlgorithms.AESGCM;
            KeyEncoding = new VisualBasicValue<Encoding>(typeof(Encoding).FullName + "." + nameof(Encoding.UTF8)); // Kinda ugly.
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

        protected override void Execute(CodeActivityContext context)
        {
            try
            {
                string inputFilePath = InputFilePath.Get(context);
                string outputFilePath = OutputFilePath.Get(context);
                string key = Key.Get(context);
                SecureString keySecureString = KeySecureString.Get(context);
                Encoding keyEncoding = KeyEncoding.Get(context);

                if (string.IsNullOrWhiteSpace(inputFilePath))
                {
                    throw new ArgumentNullException(Resources.InputFilePathDisplayName);
                }
                if (string.IsNullOrWhiteSpace(outputFilePath))
                {
                    throw new ArgumentNullException(Resources.OutputFilePathDisplayName);
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
                if (!File.Exists(inputFilePath))
                {
                    throw new ArgumentException(Resources.FileDoesNotExistsException, Resources.InputFilePathDisplayName);
                }
                // Because we use File.WriteAllText below, we don't need to delete the file now.
                if (File.Exists(outputFilePath) && !Overwrite)
                {
                    throw new ArgumentException(Resources.FileAlreadyExistsException, Resources.OutputFilePathDisplayName);
                }

                byte[] encrypted = File.ReadAllBytes(inputFilePath);

                byte[] decrypted = null;
                try
                {
                    decrypted = CryptographyHelper.DecryptData(Algorithm, encrypted, key != null ? keyEncoding.GetBytes(key) : keyEncoding.GetBytes(new NetworkCredential("", keySecureString).Password));
                }
                catch (CryptographicException ex)
                {
                    throw new InvalidOperationException(Resources.GenericCryptographicException, ex);
                }

                // This overwrites the file if it already exists.
                File.WriteAllBytes(outputFilePath, decrypted);
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.ToString());

                if (!ContinueOnError.Get(context))
                {
                    throw;
                }
            }
        }
    }
}
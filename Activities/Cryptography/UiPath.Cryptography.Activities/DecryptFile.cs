using System;
using System.Activities;
using System.Activities.Expressions;
using System.Activities.Validation;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UiPath.Cryptography.Activities.Models;
using UiPath.Cryptography.Activities.Properties;
using UiPath.Cryptography.Enums;
using UiPath.Platform.ResourceHandling;

namespace UiPath.Cryptography.Activities
{
    [LocalizedDisplayName(nameof(Resources.Activity_DecryptFile_Name))]
    [LocalizedDescription(nameof(Resources.Activity_DecryptFile_Description))]
    public partial class DecryptFile : CodeActivity
    {
        [RequiredArgument]
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_DecryptFile_Property_Algorithm_Name))]
        [LocalizedDescription(nameof(Resources.Activity_DecryptFile_Property_Algorithm_Description))]
        public SymmetricAlgorithms Algorithm { get; set; }

        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_DecryptFile_Property_InputFilePath_Name))]
        [LocalizedDescription(nameof(Resources.Activity_DecryptFile_Property_InputFilePath_Description))]
        public InArgument<string> InputFilePath { get; set; }

        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_DecryptFile_Property_Key_Name))]
        [LocalizedDescription(nameof(Resources.Activity_DecryptFile_Property_Key_Description))]
        public InArgument<string> Key { get; set; }

        [Browsable(false)]
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_DecryptFile_Property_KeyInputModeSwitch_Name))]
        [LocalizedDescription(nameof(Resources.Activity_DecryptFile_Property_KeyInputModeSwitch_Description))]
        public KeyInputMode KeyInputModeSwitch { get; set; }

        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_DecryptFile_Property_KeySecureString_Name))]
        [LocalizedDescription(nameof(Resources.Activity_DecryptFile_Property_KeySecureString_Description))]
        public InArgument<SecureString> KeySecureString { get; set; }

        [RequiredArgument]
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_DecryptFile_Property_KeyEncoding_Name))]
        [LocalizedDescription(nameof(Resources.Activity_DecryptFile_Property_KeyEncoding_Description))]
        public InArgument<Encoding> KeyEncoding { get; set; }

        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_DecryptFile_Property_OutputFilePath_Name))]
        [LocalizedDescription(nameof(Resources.Activity_DecryptFile_Property_OutputFilePath_Description))]
        public InArgument<string> OutputFilePath { get; set; }

        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_DecryptFile_Property_OutputFileName_Name))]
        [LocalizedDescription(nameof(Resources.Activity_DecryptFile_Property_OutputFileName_Description))]
        public InArgument<string> OutputFileName { get; set; }

        [RequiredArgument]
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_DecryptFile_Property_Overwrite_Name))]
        [LocalizedDescription(nameof(Resources.Activity_DecryptFile_Property_Overwrite_Description))]
        public bool Overwrite { get; set; }

        [DefaultValue(null)]
        [LocalizedCategory(nameof(Resources.Common))]
        [LocalizedDisplayName(nameof(Resources.Activity_DecryptFile_Property_ContinueOnError_Name))]
        [LocalizedDescription(nameof(Resources.Activity_DecryptFile_Property_ContinueOnError_Description))]
        public InArgument<bool> ContinueOnError { get; set; }

        [Browsable(false)]
        [DefaultValue(null)]
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_DecryptFile_Property_InputFile_Name))]
        [LocalizedDescription(nameof(Resources.Activity_DecryptFile_Property_InputFile_Description))]
        public InArgument<IResource> InputFile { get; set; }

        [Browsable(false)]
        [DefaultValue(null)]
        [LocalizedCategory(nameof(Resources.Output))]
        [LocalizedDisplayName(nameof(Resources.Activity_DecryptFile_Property_DecryptedFile_Name))]
        [LocalizedDescription(nameof(Resources.Activity_DecryptFile_Property_DecryptedFile_Description))]
        public OutArgument<ILocalResource> DecryptedFile { get; set; }

        public DecryptFile()
        {
            Algorithm = SymmetricAlgorithms.AESGCM;
            KeyEncoding = new InArgument<Encoding>(ExpressionServices.Convert((env) => System.Text.Encoding.UTF8));
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

        protected override void Execute(CodeActivityContext context)
        {
            try
            {
                var inputFilePath = InputFilePath.Get(context);
                var inputFile = InputFile.Get(context);
                var outputFilePath = OutputFilePath.Get(context);
                var outputFileName = OutputFileName.Get(context);
                var key = Key.Get(context);
                var keySecureString = KeySecureString.Get(context);
                var keyEncoding = KeyEncoding.Get(context);

                if (string.IsNullOrWhiteSpace(inputFilePath) && inputFile == null)
                    throw new ArgumentNullException(Resources.InputFilePathDisplayName);

                if (string.IsNullOrWhiteSpace(outputFilePath) && inputFile == null)
                    throw new ArgumentNullException(Resources.OutputFilePathDisplayName);

                //either input file path or input file as resource should be used
                if (!string.IsNullOrWhiteSpace(inputFilePath) && inputFile != null)
                    throw new ArgumentException(string.Format(Resources.Exception_UseOnlyFilePathOrInputResource,
                        Resources.Activity_EncryptFile_Property_InputFile_Name, Resources.Activity_EncryptFile_Property_InputFilePath_Name));

                if (string.IsNullOrWhiteSpace(key) && keySecureString == null)
                    throw new ArgumentNullException(Resources.KeyAndSecureStringNull);

                if (key != null && keySecureString != null)
                    throw new ArgumentNullException(Resources.KeyAndSecureStringNotNull);

                if (keyEncoding == null)
                    throw new ArgumentNullException(Resources.Encoding);

                if (!File.Exists(inputFilePath) && inputFile == null)
                    throw new ArgumentException(Resources.FileDoesNotExistsException, Resources.InputFilePathDisplayName);

                // Because we use File.WriteAllText below, we don't need to delete the file now.
                if (File.Exists(outputFilePath) && !Overwrite)
                    throw new ArgumentException(Resources.FileAlreadyExistsException, Resources.OutputFilePathDisplayName);

                if (inputFile != null && inputFile.IsFolder)
                    throw new ArgumentException(Resources.Exception_UseOnlyFilesNotFolders);

                var fileName = string.Empty;
                //get the input file from the Resource
                if (inputFile != null && !inputFile.IsFolder)
                {
                    // Get local file
                    var localFile = inputFile.ToLocalResource();
                    //Resolve Sync
                    Task.Run(async () => await localFile.ResolveAsync()).GetAwaiter().GetResult();

                    inputFilePath = localFile.LocalPath;
                    fileName = localFile.FullName;
                }

                if (outputFileName != null)
                {
                    fileName = outputFileName;
                }

                var encrypted = File.ReadAllBytes(inputFilePath);

                byte[] decrypted = null;
                try
                {
                    decrypted = CryptographyHelper.DecryptData(Algorithm, encrypted, CryptographyHelper.KeyEncoding(keyEncoding, key, keySecureString));
                }
                catch (CryptographicException ex)
                {
                    throw new InvalidOperationException(Resources.GenericCryptographicException, ex);
                }

                var item = new CryptographyLocalItem(decrypted, fileName);

                DecryptedFile.Set(context, item);

                if (string.IsNullOrEmpty(outputFilePath))
                {
                    outputFilePath = item.LocalPath;
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
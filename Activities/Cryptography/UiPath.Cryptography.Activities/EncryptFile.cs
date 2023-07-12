using System;
using System.Activities;
using System.Activities.Expressions;
using System.Activities.Validation;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Security;
using System.Text;
using UiPath.Cryptography.Activities.Helpers;
using UiPath.Cryptography.Activities.Models;
using UiPath.Cryptography.Activities.Properties;
using UiPath.Cryptography.Enums;
using UiPath.Platform.ResourceHandling;

#pragma warning disable CS0618 // obsolete encryption algorithm

namespace UiPath.Cryptography.Activities
{
    [LocalizedDisplayName(nameof(Resources.Activity_EncryptFile_Name))]
    [LocalizedDescription(nameof(Resources.Activity_EncryptFile_Description))]
    public partial class EncryptFile : CodeActivity
    {
        private const string Encrypted = "_Encrypted";

        public EncryptFile()
        {
            Algorithm = SymmetricAlgorithms.AESGCM;
#if NET461
            //we only use this on legacy
            KeyEncoding = new InArgument<Encoding>(ExpressionServices.Convert((env) => System.Text.Encoding.UTF8));
#endif
#if NET
            //for modern and cross projects
            KeyEncodingString = Encoding.UTF8.CodePage.ToString();
#endif
        }

        [RequiredArgument]
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_EncryptFile_Property_Algorithm_Name))]
        [LocalizedDescription(nameof(Resources.Activity_EncryptFile_Property_Algorithm_Description))]
        public SymmetricAlgorithms Algorithm { get; set; }

        [OverloadGroup(nameof(InputFilePath))]
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_EncryptFile_Property_InputFilePath_Name))]
        [LocalizedDescription(nameof(Resources.Activity_EncryptFile_Property_InputFilePath_Description))]
        public InArgument<string> InputFilePath { get; set; }

        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_EncryptFile_Property_Key_Name))]
        [LocalizedDescription(nameof(Resources.Activity_EncryptFile_Property_Key_Description))]
        public InArgument<string> Key { get; set; }

        [Browsable(false)]
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_EncryptFile_Property_FileInputModeSwitch_Name))]
        [LocalizedDescription(nameof(Resources.Activity_EncryptFile_Property_FileInputModeSwitch_Description))]
        public FileInputMode FileInputModeSwitch { get; set; }

        [Browsable(false)]
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_EncryptFile_Property_KeyInputModeSwitch_Name))]
        [LocalizedDescription(nameof(Resources.Activity_EncryptFile_Property_KeyInputModeSwitch_Description))]
        public KeyInputMode KeyInputModeSwitch { get; set; }

        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_EncryptFile_Property_KeySecureString_Name))]
        [LocalizedDescription(nameof(Resources.Activity_EncryptFile_Property_KeySecureString_Description))]
        public InArgument<SecureString> KeySecureString { get; set; }

        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_EncryptFile_Property_KeyEncoding_Name))]
        [LocalizedDescription(nameof(Resources.Activity_EncryptFile_Property_KeyEncoding_Description))]
        public InArgument<Encoding> KeyEncoding { get; set; }

        [Browsable(false)]
        public InArgument<string> KeyEncodingString { get; set; }

        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_EncryptFile_Property_OutputFilePath_Name))]
        [LocalizedDescription(nameof(Resources.Activity_EncryptFile_Property_OutputFilePath_Description))]
        public InArgument<string> OutputFilePath { get; set; }

        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_EncryptFile_Property_OutputFileName_Name))]
        [LocalizedDescription(nameof(Resources.Activity_EncryptFile_Property_OutputFileName_Description))]
        public InArgument<string> OutputFileName { get; set; }

        [RequiredArgument]
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_EncryptFile_Property_Overwrite_Name))]
        [LocalizedDescription(nameof(Resources.Activity_EncryptFile_Property_Overwrite_Description))]
        public bool Overwrite { get; set; }

        [DefaultValue(null)]
        [LocalizedCategory(nameof(Resources.Common))]
        [LocalizedDisplayName(nameof(Resources.Activity_EncryptFile_Property_ContinueOnError_Name))]
        [LocalizedDescription(nameof(Resources.Activity_EncryptFile_Property_ContinueOnError_Description))]
        public InArgument<bool> ContinueOnError { get; set; }

        [Browsable(false)]
        [RequiredArgument]
        [OverloadGroup(nameof(InputFile))]
        [DefaultValue(null)]
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_EncryptFile_Property_InputFile_Name))]
        [LocalizedDescription(nameof(Resources.Activity_EncryptFile_Property_InputFile_Description))]
        public InArgument<IResource> InputFile { get; set; }

        [Browsable(false)]
        [DefaultValue(null)]
        [LocalizedCategory(nameof(Resources.Output))]
        [LocalizedDisplayName(nameof(Resources.Activity_EncryptFile_Property_EncryptedFile_Name))]
        [LocalizedDescription(nameof(Resources.Activity_EncryptFile_Property_EncryptedFile_Description))]
        public OutArgument<ILocalResource> EncryptedFile { get; set; }

        protected override void CacheMetadata(CodeActivityMetadata metadata)
        {
            base.CacheMetadata(metadata);

            if (!CryptographyHelper.IsFipsCompliant(Algorithm))
            {
                var error = new ValidationError(Resources.FipsComplianceWarning, true, nameof(Algorithm));

                metadata.AddValidationError(error);
            }
#if NET
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
#endif
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
                var keyEncodingString = KeyEncodingString.Get(context);
#if NET
                if (string.IsNullOrWhiteSpace(key) && KeyInputModeSwitch == KeyInputMode.Key)
                {
                    throw new ArgumentNullException(Resources.Activity_EncryptFile_Property_Key_Name);
                }
                if ((keySecureString == null || keySecureString?.Length == 0) && KeyInputModeSwitch == KeyInputMode.SecureKey)
                {
                    throw new ArgumentNullException(Resources.Activity_EncryptFile_Property_KeySecureString_Name);
                }
#endif

#if NET461
                if (string.IsNullOrWhiteSpace(key) && (keySecureString == null || keySecureString?.Length == 0))
                {
                    throw new ArgumentNullException(Resources.KeyAndSecureStringNull);
                }
#endif
                if (keyEncoding == null && string.IsNullOrEmpty(keyEncodingString)) throw new ArgumentNullException(Resources.Encoding);

                if (!File.Exists(inputFilePath) && inputFile == null)
                    throw new ArgumentException(Resources.FileDoesNotExistsException,
                        Resources.InputFilePathDisplayName);

                // Because we use File.WriteAllText below, we don't need to delete the file now.
                if (File.Exists(outputFilePath) && !Overwrite)
                    throw new ArgumentException(Resources.FileAlreadyExistsException,
                        Resources.OutputFilePathDisplayName);

                if (inputFile != null && inputFile.IsFolder)
                    throw new ArgumentException(Resources.Exception_UseOnlyFilesNotFolders);

                var result = FilePathHelpers.GetDefaultFileNameAndLocation(inputFile, inputFilePath, outputFileName, Overwrite, outputFilePath, Encrypted);

                keyEncoding = EncodingHelpers.KeyEncodingOrString(keyEncoding, keyEncodingString);

                var encrypted = CryptographyHelper.EncryptData(Algorithm, File.ReadAllBytes(result.Item3),
                    CryptographyHelper.KeyEncoding(keyEncoding, key, keySecureString));

                if (string.IsNullOrEmpty(outputFilePath))
                {
                    var item = new CryptographyLocalItem(encrypted, result.Item1, result.Item2);

                    EncryptedFile.Set(context, item);

                    outputFilePath = item.LocalPath;
                }
                else
                {
                    var directory = Path.GetDirectoryName(outputFilePath);

                    if (!string.IsNullOrEmpty(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    var item = new CryptographyLocalItem(encrypted, Path.GetFileName(outputFilePath), outputFilePath);

                    EncryptedFile.Set(context, item);
                }

                // This overwrites the file if it already exists.
                File.WriteAllBytes(outputFilePath, encrypted);
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.ToString());

                if (!ContinueOnError.Get(context)) throw;
            }
        }
    }
}
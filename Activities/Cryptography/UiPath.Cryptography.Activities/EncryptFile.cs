using System;
using System.Activities;
using System.Activities.Expressions;
using System.Activities.Validation;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Security;
using System.Text;
using UiPath.Cryptography.Activities.Properties;
using UiPath.Cryptography.Enums;

namespace UiPath.Cryptography.Activities
{
    [LocalizedDisplayName(nameof(Resources.Activity_EncryptFile_Name))]
    [LocalizedDescription(nameof(Resources.Activity_EncryptFile_Description))]
    public partial class EncryptFile : CodeActivity
    {
        [RequiredArgument]
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_EncryptFile_Property_Algorithm_Name))]
        [LocalizedDescription(nameof(Resources.Activity_EncryptFile_Property_Algorithm_Description))]
        public SymmetricAlgorithms Algorithm { get; set; }

        [RequiredArgument]
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
        [LocalizedDisplayName(nameof(Resources.Activity_EncryptFile_Property_KeyInputModeSwitch_Name))]
        [LocalizedDescription(nameof(Resources.Activity_EncryptFile_Property_KeyInputModeSwitch_Description))]
        public KeyInputMode KeyInputModeSwitch { get; set; }

        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_EncryptFile_Property_KeySecureString_Name))]
        [LocalizedDescription(nameof(Resources.Activity_EncryptFile_Property_KeySecureString_Description))]
        public InArgument<SecureString> KeySecureString { get; set; }

        [RequiredArgument]
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_EncryptFile_Property_KeyEncoding_Name))]
        [LocalizedDescription(nameof(Resources.Activity_EncryptFile_Property_KeyEncoding_Description))]
        public InArgument<Encoding> KeyEncoding { get; set; }

        [RequiredArgument]
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_EncryptFile_Property_OutputFilePath_Name))]
        [LocalizedDescription(nameof(Resources.Activity_EncryptFile_Property_OutputFilePath_Description))]
        public InArgument<string> OutputFilePath { get; set; }

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

        public EncryptFile()
        {
            Algorithm = SymmetricAlgorithms.AESGCM;
            KeyEncoding = new InArgument<Encoding>(ExpressionServices.Convert((env) => System.Text.Encoding.UTF8));
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

                byte[] encrypted = CryptographyHelper.EncryptData(Algorithm, File.ReadAllBytes(inputFilePath), CryptographyHelper.KeyEncoding(keyEncoding, key, keySecureString));


                // This overwrites the file if it already exists.
                File.WriteAllBytes(outputFilePath, encrypted);
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
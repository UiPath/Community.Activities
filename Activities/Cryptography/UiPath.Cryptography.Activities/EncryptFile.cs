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
using UiPath.Shared.Activities;
#if ENABLE_DEFAULT_TELEMETRY
using UiPath.Shared.Telemetry.Services;
#endif

#pragma warning disable CS0618 // obsolete encryption algorithm

namespace UiPath.Cryptography.Activities
{
    [LocalizedDisplayName(nameof(Resources.Activity_EncryptFile_Name))]
    [LocalizedDescription(nameof(Resources.Activity_EncryptFile_Description))]
    public class EncryptFile : CodeActivity
    {
        private const string Encrypted = "_Encrypted";

        public EncryptFile()
        {
            Algorithm = EncryptionAlgorithm.AESGCM;
            KeyEncodingString = Encoding.UTF8.CodePage.ToString();
        }

        [RequiredArgument]
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_EncryptFile_Property_Algorithm_Name))]
        [LocalizedDescription(nameof(Resources.Activity_EncryptFile_Property_Algorithm_Description))]
        public EncryptionAlgorithm Algorithm { get; set; }

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

        [DefaultValue(null)]
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_EncryptFile_Property_PublicKeyFilePath_Name))]
        [LocalizedDescription(nameof(Resources.Activity_EncryptFile_Property_PublicKeyFilePath_Description))]
        public InArgument<string> PublicKeyFilePath { get; set; }

        [DefaultValue(false)]
        [LocalizedCategory(nameof(Resources.Category_Options_Name))]
        [LocalizedDisplayName(nameof(Resources.Activity_EncryptFile_Property_SignData_Name))]
        [LocalizedDescription(nameof(Resources.Activity_EncryptFile_Property_SignData_Description))]
        public bool SignData { get; set; }

        [DefaultValue(null)]
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_EncryptFile_Property_PrivateKeyFilePath_Name))]
        [LocalizedDescription(nameof(Resources.Activity_EncryptFile_Property_PrivateKeyFilePath_Description))]
        public InArgument<string> PrivateKeyFilePath { get; set; }

        [DefaultValue(null)]
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_EncryptFile_Property_Passphrase_Name))]
        [LocalizedDescription(nameof(Resources.Activity_EncryptFile_Property_Passphrase_Description))]
        public InArgument<SecureString> Passphrase { get; set; }

        protected override void CacheMetadata(CodeActivityMetadata metadata)
        {
            base.CacheMetadata(metadata);

            if (Algorithm == EncryptionAlgorithm.PGP)
            {
                return;
            }

            if (!CryptographyHelper.IsFipsCompliant(Algorithm))
            {
                var error = new ValidationError(Resources.FipsComplianceWarning, true, nameof(Algorithm));

                metadata.AddValidationError(error);
            }
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
        }

        protected override void Execute(CodeActivityContext context)
        {
#if ENABLE_DEFAULT_TELEMETRY
            var telemetryOperation = RuntimeTelemetryService.CreateExecutionOperation(this, context);
#endif

            try
            {
                var inputFilePath = InputFilePath.Get(context);
                var inputFile = InputFile.Get(context);
                var outputFilePath = OutputFilePath.Get(context);
                var outputFileName = OutputFileName.Get(context);

                ValidateSymmetricKeyParams(context, out var key, out var keySecureString, out var keyEncoding);

                if (!File.Exists(inputFilePath) && inputFile == null)
                    throw new ArgumentException(Resources.FileDoesNotExistsException, Resources.InputFilePathDisplayName);
                if (File.Exists(outputFilePath) && !Overwrite)
                    throw new ArgumentException(Resources.FileAlreadyExistsException, Resources.OutputFilePathDisplayName);
                if (inputFile != null && inputFile.IsFolder)
                    throw new ArgumentException(Resources.Exception_UseOnlyFilesNotFolders);

                var result = FilePathHelpers.GetDefaultFileNameAndLocation(inputFile, inputFilePath, outputFileName, Overwrite, outputFilePath, Encrypted);

                var encrypted = Algorithm == EncryptionAlgorithm.PGP
                    ? ExecutePgpEncrypt(context, result.Item3)
                    : CryptographyHelper.EncryptData(Algorithm, File.ReadAllBytes(result.Item3), CryptographyHelper.KeyEncoding(keyEncoding, key, keySecureString));

                WriteEncryptedOutput(context, outputFilePath, encrypted, result);

                File.WriteAllBytes(outputFilePath ?? ((ILocalResource)EncryptedFile.Get(context)).LocalPath, encrypted);
#if ENABLE_DEFAULT_TELEMETRY
                telemetryOperation.Send();
#endif
            }
            catch (Exception ex)
            {
#if ENABLE_DEFAULT_TELEMETRY
                telemetryOperation.SendWithException(ex);
#endif
                Trace.TraceError(ex.ToString());
                if (!ContinueOnError.Get(context)) throw;
            }
        }

        private void ValidateSymmetricKeyParams(CodeActivityContext context, out string key, out SecureString keySecureString, out Encoding keyEncoding)
        {
            key = null;
            keySecureString = null;
            keyEncoding = null;

            if (Algorithm == EncryptionAlgorithm.PGP) return;

            key = Key.Get(context);
            keySecureString = KeySecureString.Get(context);
            keyEncoding = KeyEncoding.Get(context);
            var keyEncodingString = KeyEncodingString.Get(context);

            if (string.IsNullOrWhiteSpace(key) && KeyInputModeSwitch == KeyInputMode.Key)
                throw new ArgumentNullException(Resources.Activity_EncryptFile_Property_Key_Name);
            if ((keySecureString == null || keySecureString?.Length == 0) && KeyInputModeSwitch == KeyInputMode.SecureKey)
                throw new ArgumentNullException(Resources.Activity_EncryptFile_Property_KeySecureString_Name);
            if (keyEncoding == null && string.IsNullOrEmpty(keyEncodingString))
                throw new ArgumentNullException(Resources.Encoding);

            keyEncoding = EncodingHelpers.KeyEncodingOrString(keyEncoding, keyEncodingString);
        }

        private byte[] ExecutePgpEncrypt(CodeActivityContext context, string inputPath)
        {
            var publicKeyFilePath = PublicKeyFilePath.Get(context);
            var privateKeyFilePath = PrivateKeyFilePath.Get(context);
            var passphrase = Passphrase.Get(context);
            var fileBytes = File.ReadAllBytes(inputPath);

            return PgpStreamHelper.WithPgpEncryptStreams(
                publicKeyFilePath, privateKeyFilePath, passphrase, SignData,
                (pubStream, privStream, pass) =>
                    CryptographyHelper.PgpEncrypt(fileBytes, pubStream, privStream, pass, SignData));
        }

        private void WriteEncryptedOutput(CodeActivityContext context, string outputFilePath, byte[] encrypted, (string, string, string) result)
        {
            if (string.IsNullOrEmpty(outputFilePath))
            {
                var item = new CryptographyLocalItem(encrypted, result.Item1, result.Item2);
                EncryptedFile.Set(context, item);
            }
            else
            {
                var directory = Path.GetDirectoryName(outputFilePath);
                if (!string.IsNullOrEmpty(directory))
                    Directory.CreateDirectory(directory);

                var item = new CryptographyLocalItem(encrypted, Path.GetFileName(outputFilePath), outputFilePath);
                EncryptedFile.Set(context, item);
            }
        }
    }
}
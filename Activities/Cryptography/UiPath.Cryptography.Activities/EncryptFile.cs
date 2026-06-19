using System;
using System.Activities;
using System.Activities.Validation;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Security;
using System.Text;
using UiPath.Cryptography.Activities.Helpers;
using UiPath.Cryptography.Activities.Models;
using UiPath.Cryptography.Activities.Properties;
using UiPath.Cryptography.Enums;
using UiPath.Platform.ResourceHandling;
#if ENABLE_DEFAULT_TELEMETRY
using UiPath.Shared.Telemetry.Services;
#endif

#pragma warning disable CS0618 // obsolete encryption algorithms (TripleDES, etc.) remain referenced for backwards compatibility

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

        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_EncryptFile_Property_InputFilePath_Name))]
        [LocalizedDescription(nameof(Resources.Activity_EncryptFile_Property_InputFilePath_Description))]
        public InArgument<string> InputFilePath { get; set; }

        [Browsable(false)]
        [DefaultValue(null)]
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_EncryptFile_Property_InputFile_Name))]
        [LocalizedDescription(nameof(Resources.Activity_EncryptFile_Property_InputFile_Description))]
        public InArgument<IResource> InputFile { get; set; }

        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_EncryptFile_Property_Key_Name))]
        [LocalizedDescription(nameof(Resources.Activity_EncryptFile_Property_Key_Description))]
        public InArgument<string> Key { get; set; }

        [Browsable(false)]
        [Obsolete("Legacy property kept for XAML back-compat with workflows that persisted the active file input mode. The activity now infers the mode from which side is bound.")]
        public FileInputMode FileInputModeSwitch { get; set; }

        [Browsable(false)]
        [Obsolete("Legacy property kept for XAML back-compat with workflows that persisted the active key input mode. The activity now infers the mode from which side is bound.")]
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
        [LocalizedDisplayName(nameof(Resources.Activity_EncryptFile_Property_Format_Name))]
        [LocalizedDescription(nameof(Resources.Activity_EncryptFile_Property_Format_Description))]
        [DefaultValue(SymmetricWireFormat.Classic)]
        public SymmetricWireFormat Format { get; set; }

        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_EncryptFile_Property_KeyFormat_Name))]
        [LocalizedDescription(nameof(Resources.Activity_EncryptFile_Property_KeyFormat_Description))]
        [DefaultValue(KeyBytesFormat.Encoded)]
        public KeyBytesFormat KeyFormat { get; set; }

        [DefaultValue(null)]
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_EncryptFile_Property_Iv_Name))]
        [LocalizedDescription(nameof(Resources.Activity_EncryptFile_Property_Iv_Description))]
        public InArgument<string> Iv { get; set; }

        [DefaultValue(null)]
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_EncryptFile_Property_KdfIterations_Name))]
        [LocalizedDescription(nameof(Resources.Activity_EncryptFile_Property_KdfIterations_Description))]
        public InArgument<int> KdfIterations { get; set; }

        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_EncryptFile_Property_AesKeySize_Name))]
        [LocalizedDescription(nameof(Resources.Activity_EncryptFile_Property_AesKeySize_Description))]
        [DefaultValue(AesKeySize.Aes256)]
        public AesKeySize AesKeySize { get; set; } = AesKeySize.Aes256;

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
        [LocalizedCategory(nameof(Resources.Category_Options_Name))]
        [LocalizedDisplayName(nameof(Resources.Activity_EncryptFile_Property_ContinueOnError_Name))]
        [LocalizedDescription(nameof(Resources.Activity_EncryptFile_Property_ContinueOnError_Description))]
        public InArgument<bool> ContinueOnError { get; set; }

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

        [Browsable(false)]
        [DefaultValue(null)]
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_EncryptFile_Property_PublicKeyFile_Name))]
        [LocalizedDescription(nameof(Resources.Activity_EncryptFile_Property_PublicKeyFile_Description))]
        public InArgument<IResource> PublicKeyFile { get; set; }

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
        public InArgument<string> Passphrase { get; set; }

        [DefaultValue(null)]
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_EncryptFile_Property_PassphraseSecureString_Name))]
        [LocalizedDescription(nameof(Resources.Activity_EncryptFile_Property_PassphraseSecureString_Description))]
        public InArgument<SecureString> PassphraseSecureString { get; set; }

        protected override void CacheMetadata(CodeActivityMetadata metadata)
        {
            base.CacheMetadata(metadata);

            if (!CryptographyHelper.IsFipsCompliant(Algorithm))
            {
                metadata.AddValidationError(new ValidationError(Resources.FipsComplianceWarning, isWarning: true, nameof(Algorithm)));
            }

            if (Algorithm == EncryptionAlgorithm.ChaCha20Poly1305 && !System.Security.Cryptography.ChaCha20Poly1305.IsSupported)
            {
                metadata.AddValidationError(new ValidationError(Resources.ChaCha20Poly1305NotSupported, isWarning: true, nameof(Algorithm)));
            }

            if (Iv != null && Format == SymmetricWireFormat.Raw && Algorithm != EncryptionAlgorithm.PGP)
            {
                metadata.AddValidationError(new ValidationError(Resources.Iv_NonceReuseWarning, isWarning: true, nameof(Iv)));
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
                    : ExecuteSymmetricEncrypt(context, File.ReadAllBytes(result.Item3), keyEncoding, key, keySecureString);

                WriteEncryptedOutput(context, outputFilePath, encrypted, result);
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

            if (string.IsNullOrWhiteSpace(key))
            {
                if (keySecureString == null || keySecureString.Length == 0)
                    throw new ArgumentNullException(nameof(Key), Resources.Activity_EncryptFile_Property_Key_Name);
                key = null; // ensure helper falls back to SecureString
            }
            if (keyEncoding == null && string.IsNullOrEmpty(keyEncodingString))
                throw new ArgumentNullException(Resources.Encoding);

            keyEncoding = EncodingHelpers.KeyEncodingOrString(keyEncoding, keyEncodingString);
        }

        private byte[] ExecuteSymmetricEncrypt(CodeActivityContext context, byte[] inputBytes, Encoding keyEncoding, string key, SecureString keySecureString)
        {
            var ivString = Iv?.Get(context);
            var iterations = KdfIterations?.Get(context) ?? 0;

            return SymmetricInteropHelper.RunSymmetricWithKeyLifecycle(
                Algorithm, Format, KeyFormat, keyEncoding,
                keyString: key, keySecureString: keySecureString,
                ivString: ivString, kdfIterations: iterations, needsIv: true,
                dispatch: (k, iv) => SymmetricInteropHelper.DispatchEncrypt(
                    Algorithm, Format, iterations, k, iv, inputBytes, AesKeySize));
        }

        private byte[] ExecutePgpEncrypt(CodeActivityContext context, string inputPath)
        {
            var publicKeyFilePath = PublicKeyFilePath.Get(context);
            var publicKeyResource = PublicKeyFile?.Get(context);
            if (string.IsNullOrEmpty(publicKeyFilePath) && publicKeyResource != null)
            {
                var localResource = publicKeyResource.ToLocalResource();
                localResource.ResolveAsync().GetAwaiter().GetResult();
                publicKeyFilePath = localResource.LocalPath;
            }
            var privateKeyFilePath = PrivateKeyFilePath.Get(context);

            string passphraseString = null;
            if (SignData)
            {
                passphraseString = Passphrase.Get(context);
                if (string.IsNullOrWhiteSpace(passphraseString))
                {
                    var secure = PassphraseSecureString.Get(context);
                    if (secure == null || secure.Length == 0)
                        throw new ArgumentNullException(nameof(Passphrase), Resources.Activity_EncryptFile_Property_Passphrase_Name);
                    passphraseString = new NetworkCredential(string.Empty, secure).Password;
                }
            }

            var fileBytes = File.ReadAllBytes(inputPath);

            return PgpStreamHelper.WithPgpEncryptStreams(
                publicKeyFilePath, privateKeyFilePath, passphraseString, SignData,
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

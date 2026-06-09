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
    [LocalizedDisplayName(nameof(Resources.Activity_DecryptFile_Name))]
    [LocalizedDescription(nameof(Resources.Activity_DecryptFile_Description))]
    public class DecryptFile : CodeActivity
    {
        private const string Decrypted = "_Decrypted";

        [RequiredArgument]
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_DecryptFile_Property_Algorithm_Name))]
        [LocalizedDescription(nameof(Resources.Activity_DecryptFile_Property_Algorithm_Description))]
        public EncryptionAlgorithm Algorithm { get; set; }

        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_DecryptFile_Property_InputFilePath_Name))]
        [LocalizedDescription(nameof(Resources.Activity_DecryptFile_Property_InputFilePath_Description))]
        public InArgument<string> InputFilePath { get; set; }

        [Browsable(false)]
        [DefaultValue(null)]
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_DecryptFile_Property_InputFile_Name))]
        [LocalizedDescription(nameof(Resources.Activity_DecryptFile_Property_InputFile_Description))]
        public InArgument<IResource> InputFile { get; set; }

        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_DecryptFile_Property_Key_Name))]
        [LocalizedDescription(nameof(Resources.Activity_DecryptFile_Property_Key_Description))]
        public InArgument<string> Key { get; set; }

        [Browsable(false)]
        [Obsolete("Legacy property kept for XAML back-compat with workflows that persisted the active key input mode. The activity now infers the mode from which side is bound.")]
        public KeyInputMode KeyInputModeSwitch { get; set; }

        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_DecryptFile_Property_KeySecureString_Name))]
        [LocalizedDescription(nameof(Resources.Activity_DecryptFile_Property_KeySecureString_Description))]
        public InArgument<SecureString> KeySecureString { get; set; }

        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_DecryptFile_Property_KeyEncoding_Name))]
        [LocalizedDescription(nameof(Resources.Activity_DecryptFile_Property_KeyEncoding_Description))]
        public InArgument<Encoding> KeyEncoding { get; set; }

        [Browsable(false)]
        public InArgument<string> KeyEncodingString { get; set; }

        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_DecryptFile_Property_Format_Name))]
        [LocalizedDescription(nameof(Resources.Activity_DecryptFile_Property_Format_Description))]
        [DefaultValue(SymmetricWireFormat.Classic)]
        public SymmetricWireFormat Format { get; set; }

        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_DecryptFile_Property_KeyFormat_Name))]
        [LocalizedDescription(nameof(Resources.Activity_DecryptFile_Property_KeyFormat_Description))]
        [DefaultValue(KeyBytesFormat.Encoded)]
        public KeyBytesFormat KeyFormat { get; set; }

        [DefaultValue(null)]
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_DecryptFile_Property_KdfIterations_Name))]
        [LocalizedDescription(nameof(Resources.Activity_DecryptFile_Property_KdfIterations_Description))]
        public InArgument<int> KdfIterations { get; set; }

        [Browsable(false)]
        [Obsolete("Legacy property kept for XAML back-compat with workflows that persisted the active file input mode. The activity now infers the mode from which side is bound.")]
        public FileInputMode FileInputModeSwitch { get; set; }

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
        [LocalizedCategory(nameof(Resources.Category_Options_Name))]
        [LocalizedDisplayName(nameof(Resources.Activity_DecryptFile_Property_ContinueOnError_Name))]
        [LocalizedDescription(nameof(Resources.Activity_DecryptFile_Property_ContinueOnError_Description))]
        public InArgument<bool> ContinueOnError { get; set; }

        [Browsable(false)]
        [DefaultValue(null)]
        [LocalizedCategory(nameof(Resources.Output))]
        [LocalizedDisplayName(nameof(Resources.Activity_DecryptFile_Property_DecryptedFile_Name))]
        [LocalizedDescription(nameof(Resources.Activity_DecryptFile_Property_DecryptedFile_Description))]
        public OutArgument<ILocalResource> DecryptedFile { get; set; }

        [DefaultValue(null)]
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_DecryptFile_Property_PrivateKeyFilePath_Name))]
        [LocalizedDescription(nameof(Resources.Activity_DecryptFile_Property_PrivateKeyFilePath_Description))]
        public InArgument<string> PrivateKeyFilePath { get; set; }

        [DefaultValue(null)]
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_DecryptFile_Property_Passphrase_Name))]
        [LocalizedDescription(nameof(Resources.Activity_DecryptFile_Property_Passphrase_Description))]
        public InArgument<string> Passphrase { get; set; }

        [DefaultValue(null)]
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_DecryptFile_Property_PassphraseSecureString_Name))]
        [LocalizedDescription(nameof(Resources.Activity_DecryptFile_Property_PassphraseSecureString_Description))]
        public InArgument<SecureString> PassphraseSecureString { get; set; }

        [DefaultValue(false)]
        [LocalizedCategory(nameof(Resources.Category_Options_Name))]
        [LocalizedDisplayName(nameof(Resources.Activity_DecryptFile_Property_VerifySignature_Name))]
        [LocalizedDescription(nameof(Resources.Activity_DecryptFile_Property_VerifySignature_Description))]
        public bool VerifySignature { get; set; }

        [DefaultValue(null)]
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_DecryptFile_Property_PublicKeyFilePath_Name))]
        [LocalizedDescription(nameof(Resources.Activity_DecryptFile_Property_PublicKeyFilePath_Description))]
        public InArgument<string> PublicKeyFilePath { get; set; }

        [Browsable(false)]
        [DefaultValue(null)]
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_DecryptFile_Property_PublicKeyFile_Name))]
        [LocalizedDescription(nameof(Resources.Activity_DecryptFile_Property_PublicKeyFile_Description))]
        public InArgument<IResource> PublicKeyFile { get; set; }

        public DecryptFile()
        {
            Algorithm = EncryptionAlgorithm.AESGCM;
            KeyEncodingString = Encoding.UTF8.CodePage.ToString();
        }

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

                var result = FilePathHelpers.GetDefaultFileNameAndLocation(inputFile, inputFilePath, outputFileName, Overwrite, outputFilePath, Decrypted);
                var encrypted = File.ReadAllBytes(result.Item3);

                var decrypted = Algorithm == EncryptionAlgorithm.PGP
                    ? ExecutePgpDecrypt(context, encrypted)
                    : ExecuteSymmetricDecrypt(context, encrypted, keyEncoding, key, keySecureString);

                WriteDecryptedOutput(context, outputFilePath, decrypted, result);
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

                if (!ContinueOnError.Get(context))
                {
                    throw;
                }
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
                    throw new ArgumentNullException(nameof(Key), Resources.Activity_DecryptFile_Property_Key_Name);
                key = null; // ensure helper falls back to SecureString
            }
            if (keyEncoding == null && string.IsNullOrEmpty(keyEncodingString))
                throw new ArgumentNullException(Resources.Encoding);

            keyEncoding = EncodingHelpers.KeyEncodingOrString(keyEncoding, keyEncodingString);
        }

        private byte[] ExecutePgpDecrypt(CodeActivityContext context, byte[] encrypted)
        {
            var privateKeyFilePath = PrivateKeyFilePath.Get(context);

            var passphraseString = Passphrase.Get(context);
            if (string.IsNullOrWhiteSpace(passphraseString))
            {
                var secure = PassphraseSecureString.Get(context);
                if (secure == null || secure.Length == 0)
                    throw new ArgumentNullException(nameof(Passphrase), Resources.Activity_DecryptFile_Property_Passphrase_Name);
                passphraseString = new NetworkCredential(string.Empty, secure).Password;
            }

            var publicKeyFilePath = PublicKeyFilePath.Get(context);
            var publicKeyResource = PublicKeyFile?.Get(context);
            if (string.IsNullOrEmpty(publicKeyFilePath) && publicKeyResource != null)
            {
                var localResource = publicKeyResource.ToLocalResource();
                localResource.ResolveAsync().GetAwaiter().GetResult();
                publicKeyFilePath = localResource.LocalPath;
            }

            return PgpStreamHelper.WithPgpDecryptStreams(
                privateKeyFilePath, passphraseString, publicKeyFilePath, VerifySignature,
                (privStream, pass, pubStream) =>
                    CryptographyHelper.PgpDecrypt(encrypted, privStream, pass, pubStream, VerifySignature));
        }

        private byte[] ExecuteSymmetricDecrypt(CodeActivityContext context, byte[] encrypted, Encoding keyEncoding, string key, SecureString keySecureString)
        {
            var iterations = KdfIterations?.Get(context) ?? 0;

            SymmetricInteropHelper.ValidateInteropSettings(Algorithm, Format, KeyFormat, ivString: null, iterations, null);

            byte[] keyOrPasswordBytes = SymmetricInteropHelper.ParseKeyOrIv(key, keySecureString, KeyFormat, keyEncoding);

            if (Format == SymmetricWireFormat.Raw)
                SymmetricInteropHelper.ValidateInteropSettings(Algorithm, Format, KeyFormat, ivString: null, iterations, keyOrPasswordBytes?.Length);

            try
            {
                return SymmetricInteropHelper.DispatchDecrypt(Algorithm, Format, iterations, keyOrPasswordBytes, encrypted);
            }
            catch (CryptographicException ex)
            {
                throw new InvalidOperationException(Resources.GenericCryptographicException, ex);
            }
            finally
            {
                SymmetricInteropHelper.ClearKeyBytes(keyOrPasswordBytes);
            }
        }

        private void WriteDecryptedOutput(CodeActivityContext context, string outputFilePath, byte[] decrypted, (string, string, string) result)
        {
            if (string.IsNullOrEmpty(outputFilePath))
            {
                var item = new CryptographyLocalItem(decrypted, result.Item1, result.Item2);
                DecryptedFile.Set(context, item);
            }
            else
            {
                var directory = Path.GetDirectoryName(outputFilePath);
                if (!string.IsNullOrEmpty(directory))
                    Directory.CreateDirectory(directory);

                var item = new CryptographyLocalItem(decrypted, Path.GetFileName(outputFilePath), outputFilePath);
                DecryptedFile.Set(context, item);
            }
        }
    }
}

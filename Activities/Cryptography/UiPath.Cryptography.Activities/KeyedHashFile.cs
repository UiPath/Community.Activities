using System;
using System.Activities;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UiPath.Cryptography.Activities.Helpers;
using UiPath.Cryptography.Activities.Properties;
using UiPath.Cryptography.Enums;
using UiPath.Platform.ResourceHandling;
using UiPath.Shared.Activities;
#if ENABLE_DEFAULT_TELEMETRY
using UiPath.Shared.Telemetry.Services;
#endif

#pragma warning disable CS0618 // CryptographyHelper is intentionally marked Obsolete to discourage external use; in-package consumers are expected.

namespace UiPath.Cryptography.Activities
{
    [LocalizedDisplayName(nameof(Resources.Activity_KeyedHashFile_Name))]
    [LocalizedDescription(nameof(Resources.Activity_KeyedHashFile_Description))]
    public class KeyedHashFile : CodeActivity<string>
    {
        [RequiredArgument]
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_KeyedHashFile_Property_Algorithm_Name))]
        [LocalizedDescription(nameof(Resources.Activity_KeyedHashFile_Property_Algorithm_Description))]
        public KeyedHashAlgorithms Algorithm { get; set; }

        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_KeyedHashFile_Property_FilePath_Name))]
        [LocalizedDescription(nameof(Resources.Activity_KeyedHashFile_Property_FilePath_Description))]
        public InArgument<string> FilePath { get; set; }

        [Browsable(false)]
        [DefaultValue(null)]
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_KeyedHashFile_Property_InputFile_Name))]
        [LocalizedDescription(nameof(Resources.Activity_KeyedHashFile_Property_InputFile_Description))]
        public InArgument<IResource> InputFile { get; set; }

        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_KeyedHashFile_Property_Key_Name))]
        [LocalizedDescription(nameof(Resources.Activity_KeyedHashFile_Property_Key_Description))]
        public InArgument<string> Key { get; set; }

        [Browsable(false)]
        [Obsolete("Legacy property kept for XAML back-compat with workflows that persisted the active file input mode. The activity now infers the mode from which side is bound.")]
        public FileInputMode FileInputModeSwitch { get; set; }

        [Browsable(false)]
        [Obsolete("Legacy property kept for XAML back-compat with workflows that persisted the active key input mode. The activity now infers the mode from which side is bound.")]
        public KeyInputMode KeyInputModeSwitch { get; set; }

        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_KeyedHashFile_Property_KeySecureString_Name))]
        [LocalizedDescription(nameof(Resources.Activity_KeyedHashFile_Property_KeySecureString_Description))]
        public InArgument<SecureString> KeySecureString { get; set; }

        [Browsable(false)]
        [LocalizedCategory(nameof(Resources.Category_Encoding_Name))]
        [LocalizedDisplayName(nameof(Resources.Activity_KeyedHashFile_Property_Encoding_Name))]
        [LocalizedDescription(nameof(Resources.Activity_KeyedHashFile_Property_Encoding_Description))]
        public InArgument<Encoding> Encoding { get; set; }

        [Browsable(false)]
        public InArgument<string> KeyEncodingString { get; set; }

        [LocalizedCategory(nameof(Resources.Output))]
        [LocalizedDisplayName(nameof(Resources.Activity_KeyedHashFile_Property_Result_Name))]
        [LocalizedDescription(nameof(Resources.Activity_KeyedHashFile_Property_Result_Description))]
        public new OutArgument<string> Result { get => base.Result; set => base.Result = value; }

        [DefaultValue(null)]
        [LocalizedCategory(nameof(Resources.Category_Options_Name))]
        [LocalizedDisplayName(nameof(Resources.Activity_KeyedHashFile_Property_ContinueOnError_Name))]
        [LocalizedDescription(nameof(Resources.Activity_KeyedHashFile_Property_ContinueOnError_Description))]
        public InArgument<bool> ContinueOnError { get; set; }

        public KeyedHashFile()
        {
            Algorithm = KeyedHashAlgorithms.HMACSHA256;
            KeyEncodingString = System.Text.Encoding.UTF8.CodePage.ToString();
        }

        protected override string Execute(CodeActivityContext context)
        {
            ITelemetryOperationWrapper telemetryOperation = null;
#if ENABLE_DEFAULT_TELEMETRY
            telemetryOperation = RuntimeTelemetryService.CreateExecutionOperation(this, context);
#endif

            string result = null;

            try
            {
                var filePath = FilePath.Get(context);
                var inputFile = InputFile.Get(context);
                var key = Key.Get(context);
                var keySecureString = KeySecureString.Get(context);
                var keyEncoding = Encoding.Get(context);
                var keyEncodingString = KeyEncodingString.Get(context);

                if (Algorithm.ToString().StartsWith(nameof(HMAC)))
                {
                    if (string.IsNullOrWhiteSpace(key))
                    {
                        if (keySecureString == null || keySecureString.Length == 0)
                            throw new ArgumentNullException(nameof(Key), Resources.Activity_KeyedHashFile_Property_Key_Name);
                        key = null; // ensure helper falls back to SecureString
                    }
                }

                if (keyEncoding == null && string.IsNullOrEmpty(keyEncodingString)) throw new ArgumentNullException(Resources.Encoding);

                if (!File.Exists(filePath) && inputFile == null)
                    throw new ArgumentException(Resources.FileDoesNotExistsException, Resources.FilePathDisplayName);
                if (inputFile != null && inputFile.IsFolder)
                    throw new ArgumentException(Resources.Exception_UseOnlyFilesNotFolders);

                if (string.IsNullOrEmpty(filePath) && inputFile != null)
                {
                    var localFile = inputFile.ToLocalResource();
                    Task.Run(async () => await localFile.ResolveAsync()).GetAwaiter().GetResult();
                    filePath = localFile.LocalPath;
                }

                keyEncoding = EncodingHelpers.KeyEncodingOrString(keyEncoding, keyEncodingString);

                var hashed = CryptographyHelper.HashDataWithKey(Algorithm, File.ReadAllBytes(filePath),
                    CryptographyHelper.KeyEncoding(keyEncoding, key, keySecureString));

                telemetryOperation?.Send();
                result = BitConverter.ToString(hashed).Replace("-", string.Empty);
            }
            catch (Exception ex)
            {
                telemetryOperation?.SendWithException(ex);
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

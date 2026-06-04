using System;
using System.Activities;
using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using UiPath.Cryptography.Activities.Helpers;
using UiPath.Cryptography.Activities.Properties;
using UiPath.Platform.ResourceHandling;
using UiPath.Shared.Activities;
#if ENABLE_DEFAULT_TELEMETRY
using UiPath.Shared.Telemetry.Services;
#endif

#pragma warning disable CS0618 // CryptographyHelper is intentionally marked Obsolete to discourage external use; in-package consumers are expected.

namespace UiPath.Cryptography.Activities
{
    [LocalizedDisplayName(nameof(Resources.Activity_PgpClearsignFile_Name))]
    [LocalizedDescription(nameof(Resources.Activity_PgpClearsignFile_Description))]
    public partial class PgpClearsignFile : UiPath.Shared.Activities.AsyncTaskCodeActivity
    {
        private const string ClearSigned = "_ClearSigned";

        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_PgpClearsignFile_Property_InputFilePath_Name))]
        [LocalizedDescription(nameof(Resources.Activity_PgpClearsignFile_Property_InputFilePath_Description))]
        public InArgument<string> InputFilePath { get; set; }

        [Browsable(false)]
        [DefaultValue(null)]
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_PgpClearsignFile_Property_InputFile_Name))]
        [LocalizedDescription(nameof(Resources.Activity_PgpClearsignFile_Property_InputFile_Description))]
        public InArgument<IResource> InputFile { get; set; }

        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_PgpClearsignFile_Property_PrivateKeyFilePath_Name))]
        [LocalizedDescription(nameof(Resources.Activity_PgpClearsignFile_Property_PrivateKeyFilePath_Description))]
        public InArgument<string> PrivateKeyFilePath { get; set; }

        [Browsable(false)]
        [DefaultValue(null)]
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_PgpClearsignFile_Property_PrivateKeyFile_Name))]
        [LocalizedDescription(nameof(Resources.Activity_PgpClearsignFile_Property_PrivateKeyFile_Description))]
        public InArgument<IResource> PrivateKeyFile { get; set; }

        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_PgpClearsignFile_Property_Passphrase_Name))]
        [LocalizedDescription(nameof(Resources.Activity_PgpClearsignFile_Property_Passphrase_Description))]
        public InArgument<string> Passphrase { get; set; }

        [DefaultValue(null)]
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_PgpClearsignFile_Property_PassphraseSecureString_Name))]
        [LocalizedDescription(nameof(Resources.Activity_PgpClearsignFile_Property_PassphraseSecureString_Description))]
        public InArgument<SecureString> PassphraseSecureString { get; set; }

        [RequiredArgument]
        [LocalizedCategory(nameof(Resources.Category_Options_Name))]
        [LocalizedDisplayName(nameof(Resources.Activity_PgpClearsignFile_Property_OutputFilePath_Name))]
        [LocalizedDescription(nameof(Resources.Activity_PgpClearsignFile_Property_OutputFilePath_Description))]
        public InArgument<string> OutputFilePath { get; set; }

        [DefaultValue(false)]
        [LocalizedCategory(nameof(Resources.Category_Options_Name))]
        [LocalizedDisplayName(nameof(Resources.Activity_PgpClearsignFile_Property_Overwrite_Name))]
        [LocalizedDescription(nameof(Resources.Activity_PgpClearsignFile_Property_Overwrite_Description))]
        public bool Overwrite { get; set; }

        [DefaultValue(null)]
        [LocalizedCategory(nameof(Resources.Category_Options_Name))]
        [LocalizedDisplayName(nameof(Resources.Activity_PgpClearsignFile_Property_ContinueOnError_Name))]
        [LocalizedDescription(nameof(Resources.Activity_PgpClearsignFile_Property_ContinueOnError_Description))]
        public InArgument<bool> ContinueOnError { get; set; }

        [Browsable(false)]
        [DefaultValue(null)]
        [LocalizedCategory(nameof(Resources.Output))]
        [LocalizedDisplayName(nameof(Resources.Activity_PgpClearsignFile_Property_ClearSignedFile_Name))]
        [LocalizedDescription(nameof(Resources.Activity_PgpClearsignFile_Property_ClearSignedFile_Description))]
        public OutArgument<ILocalResource> ClearSignedFile { get; set; }

        protected override async Task<Action<AsyncCodeActivityContext>> ExecuteAsync(
            AsyncCodeActivityContext context,
            CancellationToken cancellationToken)
        {
            var continueOnError = ContinueOnError.Get(context);
            try
            {
                var inputPath = await PgpFileResolver.ResolveAsync(
                    InputFilePath.Get(context), InputFile.Get(context),
                    nameof(InputFilePath), Resources.Activity_PgpClearsignFile_Property_InputFilePath_Name,
                    cancellationToken);

                var privateKeyPath = await PgpFileResolver.ResolveAsync(
                    PrivateKeyFilePath.Get(context), PrivateKeyFile.Get(context),
                    nameof(PrivateKeyFilePath), Resources.Activity_PgpClearsignFile_Property_PrivateKeyFilePath_Name,
                    cancellationToken);

                var passphraseString = PgpFileResolver.ResolvePassphrase(
                    Passphrase.Get(context), PassphraseSecureString.Get(context),
                    nameof(Passphrase), Resources.Activity_PgpClearsignFile_Property_Passphrase_Name);

                var outputPath = OutputFilePath.Get(context);

                var item = PgpFileSignHelper.ExecuteSign(
                    inputPath,
                    privateKeyPath,
                    passphraseString,
                    outputPath,
                    Overwrite,
                    ClearSigned,
                    CryptographyHelper.PgpClearSign);

                ClearSignedFile.Set(context, item);

#if ENABLE_DEFAULT_TELEMETRY
                var telemetryOperation = RuntimeTelemetryService.CreateExecutionOperation(this, context);
                telemetryOperation.Send();
#endif
            }
            catch (Exception ex)
            {
#if ENABLE_DEFAULT_TELEMETRY
                var telemetryOperation = RuntimeTelemetryService.CreateExecutionOperation(this, context);
                telemetryOperation.SendWithException(ex);
#endif
                Trace.TraceError(ex.ToString());
                if (!continueOnError)
                    throw;
            }

            return _ => { };
        }
    }
}

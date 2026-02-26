using System;
using System.Activities;
using System.ComponentModel;
using System.Diagnostics;
using System.Security;
using UiPath.Cryptography.Activities.Helpers;
using UiPath.Cryptography.Activities.Properties;
using UiPath.Platform.ResourceHandling;
using UiPath.Shared.Activities;
#if ENABLE_DEFAULT_TELEMETRY
using UiPath.Shared.Telemetry.Services;
#endif

namespace UiPath.Cryptography.Activities
{
    [LocalizedDisplayName(nameof(Resources.Activity_PgpClearSignFile_Name))]
    [LocalizedDescription(nameof(Resources.Activity_PgpClearSignFile_Description))]
    public partial class PgpClearSignFile : CodeActivity
    {
        private const string ClearSigned = "_ClearSigned";

        [RequiredArgument]
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_PgpClearSignFile_Property_InputFilePath_Name))]
        [LocalizedDescription(nameof(Resources.Activity_PgpClearSignFile_Property_InputFilePath_Description))]
        public InArgument<string> InputFilePath { get; set; }

        [RequiredArgument]
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_PgpClearSignFile_Property_PrivateKeyFilePath_Name))]
        [LocalizedDescription(nameof(Resources.Activity_PgpClearSignFile_Property_PrivateKeyFilePath_Description))]
        public InArgument<string> PrivateKeyFilePath { get; set; }

        [RequiredArgument]
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_PgpClearSignFile_Property_Passphrase_Name))]
        [LocalizedDescription(nameof(Resources.Activity_PgpClearSignFile_Property_Passphrase_Description))]
        public InArgument<SecureString> Passphrase { get; set; }

        [LocalizedCategory(nameof(Resources.Category_Options_Name))]
        [LocalizedDisplayName(nameof(Resources.Activity_PgpClearSignFile_Property_OutputFilePath_Name))]
        [LocalizedDescription(nameof(Resources.Activity_PgpClearSignFile_Property_OutputFilePath_Description))]
        public InArgument<string> OutputFilePath { get; set; }

        [DefaultValue(false)]
        [LocalizedCategory(nameof(Resources.Category_Options_Name))]
        [LocalizedDisplayName(nameof(Resources.Activity_PgpClearSignFile_Property_Overwrite_Name))]
        [LocalizedDescription(nameof(Resources.Activity_PgpClearSignFile_Property_Overwrite_Description))]
        public bool Overwrite { get; set; }

        [DefaultValue(null)]
        [LocalizedCategory(nameof(Resources.Common))]
        [LocalizedDisplayName(nameof(Resources.Activity_PgpClearSignFile_Property_ContinueOnError_Name))]
        [LocalizedDescription(nameof(Resources.Activity_PgpClearSignFile_Property_ContinueOnError_Description))]
        public InArgument<bool> ContinueOnError { get; set; }

        [Browsable(false)]
        [DefaultValue(null)]
        [LocalizedCategory(nameof(Resources.Output))]
        [LocalizedDisplayName(nameof(Resources.Activity_PgpClearSignFile_Property_ClearSignedFile_Name))]
        [LocalizedDescription(nameof(Resources.Activity_PgpClearSignFile_Property_ClearSignedFile_Description))]
        public OutArgument<ILocalResource> ClearSignedFile { get; set; }

        protected override void Execute(CodeActivityContext context)
        {
#if ENABLE_DEFAULT_TELEMETRY
            var telemetryOperation = RuntimeTelemetryService.CreateExecutionOperation(this, context);
#endif

            try
            {
                var item = PgpFileSignHelper.ExecuteSign(
                    InputFilePath.Get(context),
                    PrivateKeyFilePath.Get(context),
                    Passphrase.Get(context),
                    OutputFilePath.Get(context),
                    Overwrite,
                    ClearSigned,
                    CryptographyHelper.PgpClearSign);

                ClearSignedFile.Set(context, item);

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
    }
}

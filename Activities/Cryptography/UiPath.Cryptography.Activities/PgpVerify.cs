using System;
using System.Activities;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UiPath.Cryptography.Activities.Helpers;
using UiPath.Cryptography.Activities.Properties;
using UiPath.Cryptography.Enums;
using UiPath.Platform.ResourceHandling;
using UiPath.Shared.Activities;
using UiPath.Shared.Telemetry.Services;

#pragma warning disable CS0618 // CryptographyHelper is intentionally marked Obsolete to discourage external use; in-package consumers are expected.

namespace UiPath.Cryptography.Activities
{
    [LocalizedDisplayName(nameof(Resources.Activity_PgpVerify_Name))]
    [LocalizedDescription(nameof(Resources.Activity_PgpVerify_Description))]
    public partial class PgpVerify : UiPath.Shared.Activities.AsyncTaskCodeActivity
    {
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_PgpVerify_Property_Mode_Name))]
        [LocalizedDescription(nameof(Resources.Activity_PgpVerify_Property_Mode_Description))]
        public PgpVerifyMode Mode { get; set; } = PgpVerifyMode.Signature;

        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_PgpVerify_Property_InputFilePath_Name))]
        [LocalizedDescription(nameof(Resources.Activity_PgpVerify_Property_InputFilePath_Description))]
        public InArgument<string> InputFilePath { get; set; }

        [Browsable(false)]
        [DefaultValue(null)]
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_PgpVerify_Property_InputFile_Name))]
        [LocalizedDescription(nameof(Resources.Activity_PgpVerify_Property_InputFile_Description))]
        public InArgument<IResource> InputFile { get; set; }

        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_PgpVerify_Property_PublicKeyFilePath_Name))]
        [LocalizedDescription(nameof(Resources.Activity_PgpVerify_Property_PublicKeyFilePath_Description))]
        public InArgument<string> PublicKeyFilePath { get; set; }

        [Browsable(false)]
        [DefaultValue(null)]
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_PgpVerify_Property_PublicKeyFile_Name))]
        [LocalizedDescription(nameof(Resources.Activity_PgpVerify_Property_PublicKeyFile_Description))]
        public InArgument<IResource> PublicKeyFile { get; set; }

        [DefaultValue(null)]
        [LocalizedCategory(nameof(Resources.Category_Options_Name))]
        [LocalizedDisplayName(nameof(Resources.Activity_PgpVerify_Property_ContinueOnError_Name))]
        [LocalizedDescription(nameof(Resources.Activity_PgpVerify_Property_ContinueOnError_Description))]
        public InArgument<bool> ContinueOnError { get; set; }

        [LocalizedCategory(nameof(Resources.Output))]
        [LocalizedDisplayName(nameof(Resources.Activity_PgpVerify_Property_Result_Name))]
        [LocalizedDescription(nameof(Resources.Activity_PgpVerify_Property_Result_Description))]
        public OutArgument<bool> Result { get; set; }

        protected override async Task<Action<AsyncCodeActivityContext>> ExecuteAsync(
            AsyncCodeActivityContext context,
            CancellationToken cancellationToken)
        {
            var continueOnError = ContinueOnError.Get(context);
            ITelemetryOperationWrapper telemetryOperation = null;
#if ENABLE_DEFAULT_TELEMETRY
            telemetryOperation = RuntimeTelemetryService.CreateExecutionOperation(this, context);
#endif
            try
            {
                var publicKeyPath = await PgpFileResolver.ResolveAsync(
                    PublicKeyFilePath.Get(context), PublicKeyFile.Get(context),
                    nameof(PublicKeyFilePath), Resources.Activity_PgpVerify_Property_PublicKeyFilePath_Name,
                    cancellationToken);
                if (!File.Exists(publicKeyPath))
                    throw new ArgumentException(Resources.FileDoesNotExistsException, Resources.Activity_PgpVerify_Property_PublicKeyFilePath_Name);

                bool result = false;

                if (Mode == PgpVerifyMode.PublicKey)
                {
                    using (var publicKeyStream = File.OpenRead(publicKeyPath))
                    {
                        result = CryptographyHelper.PgpVerifyPublicKey(publicKeyStream);
                        telemetryOperation?.Send();
                    }
                }
                else
                {
                    var inputPath = await PgpFileResolver.ResolveAsync(
                        InputFilePath.Get(context), InputFile.Get(context),
                        nameof(InputFilePath), Resources.Activity_PgpVerify_Property_InputFilePath_Name,
                        cancellationToken);
                    if (!File.Exists(inputPath))
                        throw new ArgumentException(Resources.FileDoesNotExistsException, Resources.Activity_PgpVerify_Property_InputFilePath_Name);

                    var inputBytes = File.ReadAllBytes(inputPath);

                    using (var publicKeyStream = File.OpenRead(publicKeyPath))
                    {
                        switch (Mode)
                        {
                            case PgpVerifyMode.Signature:
                                result = CryptographyHelper.PgpVerify(inputBytes, publicKeyStream);
                                break;
                            case PgpVerifyMode.ClearSignature:
                                result = CryptographyHelper.PgpVerifyClear(inputBytes, publicKeyStream);
                                break;
                            default:
                                throw new ArgumentOutOfRangeException(Resources.Activity_PgpVerify_Property_Mode_Name);
                        }

                        telemetryOperation?.Send();
                    }
                }

                return ctx => ctx.SetValue(Result, result);
            }
            catch (Exception ex)
            {
                telemetryOperation?.SendWithException(ex);
                Trace.TraceError(ex.ToString());

                if (!continueOnError)
                    throw;

                // On error with continueOnError=true, set Result to false
                return ctx => ctx.SetValue(Result, false);
            }
        }
    }
}

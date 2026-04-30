using System;
using System.Activities;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using UiPath.Cryptography.Activities.Properties;
using UiPath.Cryptography.Enums;
using UiPath.Shared.Activities;
#if ENABLE_DEFAULT_TELEMETRY
using UiPath.Shared.Telemetry.Services;
#endif

namespace UiPath.Cryptography.Activities
{
    [LocalizedDisplayName(nameof(Resources.Activity_PgpVerify_Name))]
    [LocalizedDescription(nameof(Resources.Activity_PgpVerify_Description))]
    public partial class PgpVerify : CodeActivity<bool>
    {
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_PgpVerify_Property_Mode_Name))]
        [LocalizedDescription(nameof(Resources.Activity_PgpVerify_Property_Mode_Description))]
        public PgpVerifyMode Mode { get; set; } = PgpVerifyMode.Signature;

        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_PgpVerify_Property_InputFilePath_Name))]
        [LocalizedDescription(nameof(Resources.Activity_PgpVerify_Property_InputFilePath_Description))]
        public InArgument<string> InputFilePath { get; set; }

        [RequiredArgument]
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.Activity_PgpVerify_Property_PublicKeyFilePath_Name))]
        [LocalizedDescription(nameof(Resources.Activity_PgpVerify_Property_PublicKeyFilePath_Description))]
        public InArgument<string> PublicKeyFilePath { get; set; }

        [DefaultValue(null)]
        [LocalizedCategory(nameof(Resources.Common))]
        [LocalizedDisplayName(nameof(Resources.Activity_PgpVerify_Property_ContinueOnError_Name))]
        [LocalizedDescription(nameof(Resources.Activity_PgpVerify_Property_ContinueOnError_Description))]
        public InArgument<bool> ContinueOnError { get; set; }

        [LocalizedCategory(nameof(Resources.Output))]
        [LocalizedDisplayName(nameof(Resources.Activity_PgpVerify_Property_Result_Name))]
        [LocalizedDescription(nameof(Resources.Activity_PgpVerify_Property_Result_Description))]
        public new OutArgument<bool> Result { get => base.Result; set => base.Result = value; }

        protected override bool Execute(CodeActivityContext context)
        {
#if ENABLE_DEFAULT_TELEMETRY
            var telemetryOperation = RuntimeTelemetryService.CreateExecutionOperation(this, context);
#endif

            try
            {
                var publicKeyFilePath = PublicKeyFilePath.Get(context);

                if (string.IsNullOrWhiteSpace(publicKeyFilePath))
                    throw new ArgumentNullException(Resources.Activity_PgpVerify_Property_PublicKeyFilePath_Name);
                if (!File.Exists(publicKeyFilePath))
                    throw new ArgumentException(Resources.FileDoesNotExistsException, Resources.Activity_PgpVerify_Property_PublicKeyFilePath_Name);

                if (Mode == PgpVerifyMode.PublicKey)
                {
                    using (var publicKeyStream = File.OpenRead(publicKeyFilePath))
                    {
                        var result = CryptographyHelper.PgpVerifyPublicKey(publicKeyStream);
#if ENABLE_DEFAULT_TELEMETRY
                        telemetryOperation.Send();
#endif
                        return result;
                    }
                }

                var inputFilePath = InputFilePath.Get(context);

                if (string.IsNullOrWhiteSpace(inputFilePath))
                    throw new ArgumentNullException(Resources.Activity_PgpVerify_Property_InputFilePath_Name);
                if (!File.Exists(inputFilePath))
                    throw new ArgumentException(Resources.FileDoesNotExistsException, Resources.Activity_PgpVerify_Property_InputFilePath_Name);

                var inputBytes = File.ReadAllBytes(inputFilePath);

                using (var publicKeyStream = File.OpenRead(publicKeyFilePath))
                {
                    bool result;
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

#if ENABLE_DEFAULT_TELEMETRY
                    telemetryOperation.Send();
#endif
                    return result;
                }
            }
            catch (Exception ex)
            {
#if ENABLE_DEFAULT_TELEMETRY
                telemetryOperation.SendWithException(ex);
#endif
                Trace.TraceError(ex.ToString());
                if (!ContinueOnError.Get(context)) throw;
                return false;
            }
        }
    }
}

using UiPath.Cryptography.Properties;

namespace UiPath.Cryptography.Enums
{
    public enum PgpVerifyMode
    {
        [LocalizedDescription(nameof(Resources.PgpVerifyMode_Signature))]
        Signature,

        [LocalizedDescription(nameof(Resources.PgpVerifyMode_ClearSignature))]
        ClearSignature,

        [LocalizedDescription(nameof(Resources.PgpVerifyMode_PublicKey))]
        PublicKey
    }
}

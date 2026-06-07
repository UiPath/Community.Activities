using System;

namespace UiPath.Cryptography.Activities.API
{
    /// <summary>
    /// A matched OpenPGP public/private key pair. The two halves are mathematically tied
    /// to a single RNG draw and must come from the same <c>PgpGenerateKeys</c> call —
    /// there is intentionally no API to construct one from independently-generated halves.
    /// Call <see cref="PgpPublicKey.Save"/> / <see cref="PgpPrivateKey.Save"/> on each
    /// half to persist if needed.
    /// </summary>
    public sealed class PgpKeyPair
    {
        public PgpPublicKey PublicKey { get; }
        public PgpPrivateKey PrivateKey { get; }

        public PgpKeyPair(PgpPublicKey publicKey, PgpPrivateKey privateKey)
        {
            ArgumentNullException.ThrowIfNull(publicKey);
            ArgumentNullException.ThrowIfNull(privateKey);
            PublicKey = publicKey;
            PrivateKey = privateKey;
        }

        public void Deconstruct(out PgpPublicKey publicKey, out PgpPrivateKey privateKey)
        {
            publicKey = PublicKey;
            privateKey = PrivateKey;
        }
    }
}

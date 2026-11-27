using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using UiPath.FTP.Extensibility;

namespace UiPath.FTP.Extended
{
    /// <summary>The <c>ssh-dss</c> host key algorithm (DSA with SHA-1). It verifies the server's signature only.</summary>
    internal sealed class DsaHostKeyAlgorithm : ISftpHostKeyAlgorithm
    {
        public string Name => "ssh-dss";

        public bool Verify(IReadOnlyList<byte[]> publicKeyValues, byte[] data, byte[] signature)
        {
            // The signature is r and s as two 160-bit values.
            if (publicKeyValues == null || publicKeyValues.Count != 4 || signature == null || signature.Length != 40)
            {
                return false;
            }

            byte[] p = publicKeyValues[0];
            byte[] q = publicKeyValues[1];
            byte[] g = publicKeyValues[2];
            byte[] y = publicKeyValues[3];
            if (q.Length != 20 || p.Length > 128 || g.Length > p.Length || y.Length > p.Length)
            {
                return false;
            }

            try
            {
                using (DSA dsa = DSA.Create()) // NOSONAR - ssh-dss (SHA-1) is a legacy host key kept for old servers, offered last, extended package only
                {
                    // The platform wants g and y as long as p.
                    dsa.ImportParameters(new DSAParameters { P = p, Q = q, G = PadLeft(g, p.Length), Y = PadLeft(y, p.Length) });
                    return dsa.VerifyData(data, signature, HashAlgorithmName.SHA1, DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
                }
            }
            catch (CryptographicException)
            {
                return false;
            }
        }

        private static byte[] PadLeft(byte[] value, int length)
        {
            if (value.Length == length)
            {
                return value;
            }

            var padded = new byte[length];
            Buffer.BlockCopy(value, 0, padded, length - value.Length, value.Length);
            return padded;
        }
    }
}

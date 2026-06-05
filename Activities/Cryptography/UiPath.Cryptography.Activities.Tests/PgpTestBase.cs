using System;
using System.IO;
using System.Security;
using PgpCore;
using UiPath.Cryptography.Enums;

namespace UiPath.Cryptography.Activities.Tests
{
    public abstract class PgpTestBase : IDisposable
    {
        protected readonly string _publicKeyPath;
        protected readonly string _privateKeyPath;
        protected const string Passphrase = "testpassphrase";
        private bool _disposed;

        protected PgpTestBase(RsaKeySize keySize = RsaKeySize.Rsa4096)
        {
            var prefix = GetType().Name;
            _publicKeyPath = Path.Combine(Path.GetTempPath(), $"{prefix}_public_{Guid.NewGuid()}.asc");
            _privateKeyPath = Path.Combine(Path.GetTempPath(), $"{prefix}_private_{Guid.NewGuid()}.asc");

            using (var pgp = new PGP())
            {
                pgp.GenerateKey(
                    new FileInfo(_publicKeyPath),
                    new FileInfo(_privateKeyPath),
                    "test@test.com",
                    Passphrase,
                    (int)keySize);
            }
        }

        protected static SecureString GetPassphraseSecureString()
        {
            return TestingHelper.StringToSecureString(Passphrase);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;
            if (disposing)
            {
                if (File.Exists(_publicKeyPath)) File.Delete(_publicKeyPath);
                if (File.Exists(_privateKeyPath)) File.Delete(_privateKeyPath);
            }
            _disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}

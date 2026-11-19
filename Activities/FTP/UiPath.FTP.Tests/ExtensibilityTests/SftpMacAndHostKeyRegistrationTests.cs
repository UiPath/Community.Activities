using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using Renci.SshNet;
using Renci.SshNet.Security;
using UiPath.FTP.Extensibility;
using Xunit;

namespace UiPath.FTP.Tests
{
    public class SftpMacAndHostKeyRegistrationTests
    {
        private sealed class RecordingHostKey : ISftpHostKeyAlgorithm
        {
            public IReadOnlyList<byte[]> Values;
            public byte[] Data;
            public byte[] Signature;
            public bool Result = true;

            public string Name => "test-multi-value";

            public bool Verify(IReadOnlyList<byte[]> publicKeyValues, byte[] data, byte[] signature)
            {
                Values = publicKeyValues;
                Data = data;
                Signature = signature;
                return Result;
            }
        }

        private sealed class Extension : ISftpCryptoExtension
        {
            private readonly Action<ISftpCryptoRegistry> _register;

            public Extension(Action<ISftpCryptoRegistry> register) => _register = register;

            public string Name => "test";

            public void Register(ISftpCryptoRegistry registry) => _register(registry);
        }

        private static ConnectionInfo Apply(Action<ISftpCryptoRegistry> register)
        {
            var info = new ConnectionInfo("localhost", 22, "user", new PasswordAuthenticationMethod("user", "password"));
            SftpCryptoRegistry.Create(new[] { new Extension(register) }).ApplyTo(info);
            return info;
        }

        [Fact]
        public void AddMac_WithoutTheFlag_IsNotEncryptThenMac_AndWithItIs()
        {
            ConnectionInfo info = Apply(r =>
            {
                r.AddMac("test-mac", 128, key => new HMACMD5(key));
                r.AddMac("test-mac-etm@openssh.com", 128, key => new HMACMD5(key), isEncryptThenMac: true);
            });

            Assert.False(info.HmacAlgorithms["test-mac"].IsEncryptThenMAC);
            Assert.True(info.HmacAlgorithms["test-mac-etm@openssh.com"].IsEncryptThenMAC);
            Assert.Equal(128, info.HmacAlgorithms["test-mac"].KeySize);
            using (HashAlgorithm mac = info.HmacAlgorithms["test-mac"].HashAlgorithm(new byte[32]))
            {
                Assert.Equal(128, mac.HashSize);
            }
        }

        [Fact]
        public void AddMac_NullArguments_Throw()
        {
            var registry = SftpCryptoRegistry.Create(null);

            Assert.Throws<ArgumentNullException>(() => registry.AddMac(null, 128, key => new HMACMD5(key), true));
            Assert.Throws<ArgumentNullException>(() => registry.AddMac("x", 128, null, true));
            Assert.Throws<ArgumentNullException>(() => registry.AddHostKeyAlgorithm(null));
        }

        private static byte[] SshString(byte[] value) =>
            new byte[] { (byte)(value.Length >> 24), (byte)(value.Length >> 16), (byte)(value.Length >> 8), (byte)value.Length }.Concat(value).ToArray();

        [Fact]
        public void AddHostKeyAlgorithm_PassesThePublicKeyValuesAndTheSignatureToTheAlgorithm()
        {
            var algorithm = new RecordingHostKey();
            ConnectionInfo info = Apply(r => r.AddHostKeyAlgorithm(algorithm));

            // The host key blob as the server sends it: the name, then the values as mpints.
            byte[][] values = { new byte[] { 0x01, 0x02, 0x03 }, new byte[] { 0x7f }, new byte[] { 0x80, 0x00, 0x01 }, new byte[] { 0x55, 0x66 } };
            byte[] blob = SshString(Encoding.ASCII.GetBytes("test-multi-value"))
                .Concat(values.SelectMany(v => SshString(v[0] >= 0x80 ? new byte[] { 0 }.Concat(v).ToArray() : v))).ToArray();
            KeyHostAlgorithm hostKey = info.HostKeyAlgorithms["test-multi-value"](blob);

            // The signature blob: the algorithm name, then the raw signature.
            byte[] rawSignature = Enumerable.Range(1, 40).Select(i => (byte)i).ToArray();
            byte[] signatureBlob = SshString(Encoding.ASCII.GetBytes("test-multi-value")).Concat(SshString(rawSignature)).ToArray();
            byte[] signedData = Encoding.ASCII.GetBytes("exchange hash");

            Assert.True(hostKey.VerifySignature(signedData, signatureBlob));
            Assert.Equal(4, algorithm.Values.Count);
            Assert.Equal(values[0], algorithm.Values[0]);
            Assert.Equal(values[1], algorithm.Values[1]);
            Assert.Equal(values[2], algorithm.Values[2]);
            Assert.Equal(values[3], algorithm.Values[3]);
            Assert.Equal(signedData, algorithm.Data);
            Assert.Equal(rawSignature, algorithm.Signature);
        }

        [Fact]
        public void AddHostKeyAlgorithm_ReportsAFailedVerification()
        {
            var algorithm = new RecordingHostKey { Result = false };
            ConnectionInfo info = Apply(r => r.AddHostKeyAlgorithm(algorithm));
            byte[] blob = SshString(Encoding.ASCII.GetBytes("test-multi-value")).Concat(SshString(new byte[] { 5 })).ToArray();
            byte[] signatureBlob = SshString(Encoding.ASCII.GetBytes("test-multi-value")).Concat(SshString(new byte[40])).ToArray();

            KeyHostAlgorithm hostKey = info.HostKeyAlgorithms["test-multi-value"](blob);

            Assert.False(hostKey.VerifySignature(new byte[] { 1 }, signatureBlob));
        }

        [Fact]
        public void AddHostKeyAlgorithm_CannotSign()
        {
            ConnectionInfo info = Apply(r => r.AddHostKeyAlgorithm(new RecordingHostKey()));
            byte[] blob = SshString(Encoding.ASCII.GetBytes("test-multi-value")).Concat(SshString(new byte[] { 5 })).ToArray();

            KeyHostAlgorithm hostKey = info.HostKeyAlgorithms["test-multi-value"](blob);

            Assert.Throws<InvalidOperationException>(() => hostKey.Sign(new byte[] { 1 }));
        }
    }
}

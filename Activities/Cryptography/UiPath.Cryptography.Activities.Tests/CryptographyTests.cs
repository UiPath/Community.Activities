using Microsoft.Activities.UnitTesting;
using Microsoft.VisualBasic.Activities;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace UiPath.Cryptography.Activities.Tests
{
    public class CryptographyTests
    {
        [Theory]
        [InlineData(HashAlgorithms.MD5)]
        [InlineData(HashAlgorithms.RIPEMD160)]
        [InlineData(HashAlgorithms.SHA1)]
        [InlineData(HashAlgorithms.SHA256)]
        [InlineData(HashAlgorithms.SHA384)]
        [InlineData(HashAlgorithms.SHA512)]
        public void HashAlgorithmsMatch(HashAlgorithms enumValue)
        {
            string toHash = "`~1234567890-=qwertyuiop[]\\ASDFGHJKL:\"ZXCVBNM<>?ăîșțâ";

            HashText hash = new HashText
            {
                Algorithm = enumValue,
                Encoding = new VisualBasicValue<Encoding>(typeof(Encoding).FullName + "." + nameof(Encoding.Unicode))
            };
            Dictionary<string, object> arguments = new Dictionary<string, object>();
            arguments.Add(nameof(HashText.Input), toHash);

            WorkflowInvokerTest invoker = new WorkflowInvokerTest(hash);
            string activityString = (string)invoker.TestActivity(arguments)[nameof(hash.Result)];

            byte[] algorithmBytes = CryptographyHelper.HashData(enumValue, Encoding.Unicode.GetBytes(toHash));

            Assert.Equal(activityString, BitConverter.ToString(algorithmBytes).Replace("-", string.Empty));
        }

        [Theory]
        [InlineData(KeyedHashAlgorithms.HMACMD5)]
        [InlineData(KeyedHashAlgorithms.HMACRIPEMD160)]
        [InlineData(KeyedHashAlgorithms.HMACSHA1)]
        [InlineData(KeyedHashAlgorithms.HMACSHA256)]
        [InlineData(KeyedHashAlgorithms.HMACSHA384)]
        [InlineData(KeyedHashAlgorithms.HMACSHA512)]
        [InlineData(KeyedHashAlgorithms.MACTripleDES)]
        public void KeyedHashAlgorithmsMatch(KeyedHashAlgorithms enumValue)
        {
            string toHash = "`~1234567890-=qwertyuiop[]\\ASDFGHJKL:\"ZXCVBNM<>?ăîșțâ";
            string key = "{>@#F09\0";

            KeyedHashText keyedHash = new KeyedHashText
            {
                Algorithm = enumValue,
                Encoding = new VisualBasicValue<Encoding>(typeof(Encoding).FullName + "." + nameof(Encoding.Unicode))
            };
            Dictionary<string, object> arguments = new Dictionary<string, object>();
            arguments.Add(nameof(KeyedHashText.Input), toHash);
            arguments.Add(nameof(KeyedHashText.Key), key);

            WorkflowInvokerTest invoker = new WorkflowInvokerTest(keyedHash);
            string activityString = (string)invoker.TestActivity(arguments)[nameof(keyedHash.Result)];

            byte[] algorithmBytes = CryptographyHelper.HashDataWithKey(enumValue, Encoding.Unicode.GetBytes(toHash), Encoding.Unicode.GetBytes(key));

            Assert.Equal(activityString, BitConverter.ToString(algorithmBytes).Replace("-", string.Empty));
        }

        [Theory]
        [InlineData(SymmetricAlgorithms.AES)]
        [InlineData(SymmetricAlgorithms.DES)]
        [InlineData(SymmetricAlgorithms.RC2)]
        [InlineData(SymmetricAlgorithms.Rijndael)]
        [InlineData(SymmetricAlgorithms.TripleDES)]
        public void SymmetricAlgorithmsEncryptionMatches(SymmetricAlgorithms enumValue)
        {
            string toProcess = "`~1234567890-=qwertyuiop[]\\ASDFGHJKL:\"ZXCVBNM<>?ăîșțâ";
            string key = "{>@#F09\0";

            EncryptText symmetricAlgorithm = new EncryptText
            {
                Algorithm = enumValue,
                Encoding = new VisualBasicValue<Encoding>(typeof(Encoding).FullName + "." + nameof(Encoding.Unicode))
            };
            Dictionary<string, object> arguments = new Dictionary<string, object>();
            arguments.Add(nameof(EncryptText.Input), toProcess);
            arguments.Add(nameof(EncryptText.Key), key);

            WorkflowInvokerTest invoker = new WorkflowInvokerTest(symmetricAlgorithm);
            string activityString = (string)invoker.TestActivity(arguments)[nameof(symmetricAlgorithm.Result)];

            byte[] algorithmBytes = CryptographyHelper.DecryptData(enumValue, Convert.FromBase64String(activityString), Encoding.Unicode.GetBytes(key));

            Assert.Equal(toProcess, Encoding.Unicode.GetString(algorithmBytes));
        }

        [Theory]
        [InlineData(SymmetricAlgorithms.AES)]
        [InlineData(SymmetricAlgorithms.DES)]
        [InlineData(SymmetricAlgorithms.RC2)]
        [InlineData(SymmetricAlgorithms.Rijndael)]
        [InlineData(SymmetricAlgorithms.TripleDES)]
        public void SymmetricAlgorithmsDecryptionMatches(SymmetricAlgorithms enumValue)
        {
            string toProcess = "`~1234567890-=qwertyuiop[]\\ASDFGHJKL:\"ZXCVBNM<>?ăîșțâ";
            string key = "{>@#F09\0";

            byte[] algorithmBytes = CryptographyHelper.EncryptData(enumValue, Encoding.Unicode.GetBytes(toProcess), Encoding.Unicode.GetBytes(key));

            DecryptText symmetricAlgorithm = new DecryptText
            {
                Algorithm = enumValue,
                Encoding = new VisualBasicValue<Encoding>(typeof(Encoding).FullName + "." + nameof(Encoding.Unicode))
            };

            Dictionary<string, object> arguments = new Dictionary<string, object>();
            arguments.Add(nameof(DecryptText.Input), Convert.ToBase64String(algorithmBytes));
            arguments.Add(nameof(DecryptText.Key), key);

            WorkflowInvokerTest invoker = new WorkflowInvokerTest(symmetricAlgorithm);
            string activityString = (string)invoker.TestActivity(arguments)[nameof(symmetricAlgorithm.Result)];

            Assert.Equal(toProcess, activityString);
        }
    }
}

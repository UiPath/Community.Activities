using System;
using System.Activities;
using System.Activities.Expressions;
using System.Collections.Generic;
using System.Security;
using System.Text;
using UiPath.Cryptography.Enums;
using Xunit;

namespace UiPath.Cryptography.Activities.Tests
{
    public class CryptographyTests
    {
#if NETFRAMEWORK

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
                Encoding = new InArgument<Encoding>(ExpressionServices.Convert((env) => System.Text.Encoding.Unicode))
            };
            Dictionary<string, object> arguments = new Dictionary<string, object>();
            arguments.Add(nameof(HashText.Input), toHash);

            WorkflowInvoker invoker = new WorkflowInvoker(hash);
            string activityString = (string)invoker.Invoke(arguments)[nameof(hash.Result)];

            byte[] algorithmBytes = CryptographyHelper.HashData(enumValue, Encoding.Unicode.GetBytes(toHash));

            Assert.Equal(activityString, BitConverter.ToString(algorithmBytes).Replace("-", string.Empty));
        }

#endif

        [Theory]
        [InlineData(KeyedHashAlgorithms.HMACMD5)]
        [InlineData(KeyedHashAlgorithms.HMACSHA1)]
        [InlineData(KeyedHashAlgorithms.HMACSHA256)]
        [InlineData(KeyedHashAlgorithms.HMACSHA384)]
        [InlineData(KeyedHashAlgorithms.HMACSHA512)]
#if NETFRAMEWORK
        [InlineData(KeyedHashAlgorithms.MACTripleDES)]
        [InlineData(KeyedHashAlgorithms.HMACRIPEMD160)]
#endif
        public void KeyedHashAlgorithmsMatch(KeyedHashAlgorithms enumValue)
        {
            string toHash = "`~1234567890-=qwertyuiop[]\\ASDFGHJKL:\"ZXCVBNM<>?ăîșțâ";
            string key = "{>@#F09\0";

            KeyedHashText keyedHash = new KeyedHashText
            {
                Algorithm = enumValue,
                Encoding = new InArgument<Encoding>(ExpressionServices.Convert((env) => System.Text.Encoding.Unicode))
            };
            Dictionary<string, object> arguments = new Dictionary<string, object>();
            arguments.Add(nameof(KeyedHashText.Input), toHash);
            arguments.Add(nameof(KeyedHashText.Key), key);

            WorkflowInvoker invoker = new WorkflowInvoker(keyedHash);
            string activityString = (string)invoker.Invoke(arguments)[nameof(keyedHash.Result)];

            byte[] algorithmBytes = CryptographyHelper.HashDataWithKey(enumValue, Encoding.Unicode.GetBytes(toHash), Encoding.Unicode.GetBytes(key));

            Assert.Equal(activityString, BitConverter.ToString(algorithmBytes).Replace("-", string.Empty));
        }

        [Theory]
        [InlineData(SymmetricAlgorithms.AES)]
        [InlineData(SymmetricAlgorithms.AESGCM)]
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
                Encoding = new InArgument<Encoding>(ExpressionServices.Convert((env) => System.Text.Encoding.Unicode))
            };
            Dictionary<string, object> arguments = new Dictionary<string, object>();
            arguments.Add(nameof(EncryptText.Input), toProcess);
            arguments.Add(nameof(EncryptText.Key), key);

            WorkflowInvoker invoker = new WorkflowInvoker(symmetricAlgorithm);
            string activityString = (string)invoker.Invoke(arguments)[nameof(symmetricAlgorithm.Result)];

            byte[] algorithmBytes = CryptographyHelper.DecryptData(enumValue, Convert.FromBase64String(activityString), Encoding.Unicode.GetBytes(key));

            Assert.Equal(toProcess, Encoding.Unicode.GetString(algorithmBytes));
        }

        [Theory]
        [InlineData(SymmetricAlgorithms.AES)]
        [InlineData(SymmetricAlgorithms.AESGCM)]
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
                Encoding = new InArgument<Encoding>(ExpressionServices.Convert((env) => System.Text.Encoding.Unicode))
            };

            Dictionary<string, object> arguments = new Dictionary<string, object>();
            arguments.Add(nameof(DecryptText.Input), Convert.ToBase64String(algorithmBytes));
            arguments.Add(nameof(DecryptText.Key), key);

            WorkflowInvoker invoker = new WorkflowInvoker(symmetricAlgorithm);
            string activityString = (string)invoker.Invoke(arguments)[nameof(symmetricAlgorithm.Result)];

            Assert.Equal(toProcess, activityString);
        }

        [Theory]
        [InlineData("Hello World Is Not Enough", "8888f610-529b-4cbb-be1a-20b095402faf")]
        [InlineData("This is just a test", "97dbfca4-7a3c-4fa3-90f3-d17603bbc4b7")]
        public void AesGcmEncryptionMatches(string plainText, string key)
        {
            var encrypted = CryptographyHelper.EncryptData(SymmetricAlgorithms.AESGCM, Encoding.UTF8.GetBytes(plainText), Encoding.UTF8.GetBytes(key));
            var decrypted = CryptographyHelper.DecryptData(SymmetricAlgorithms.AESGCM, encrypted, Encoding.UTF8.GetBytes(key));
            Assert.Equal(Encoding.UTF8.GetString(decrypted), plainText);
        }


        [Theory]
        [InlineData(KeyedHashAlgorithms.HMACMD5)]
        [InlineData(KeyedHashAlgorithms.HMACSHA1)]
        [InlineData(KeyedHashAlgorithms.HMACSHA256)]
        [InlineData(KeyedHashAlgorithms.HMACSHA384)]
        [InlineData(KeyedHashAlgorithms.HMACSHA512)]
#if NETFRAMEWORK
        [InlineData(KeyedHashAlgorithms.MACTripleDES)]
        [InlineData(KeyedHashAlgorithms.HMACRIPEMD160)]
#endif
        public void KeyedHashAlgorithmsMatchWithSecureString(KeyedHashAlgorithms enumValue)
        {
            string toHash = "`~1234567890-=qwertyuiop[]\\ASDFGHJKL:\"ZXCVBNM<>?ăîșțâ";
            SecureString keySecureString = TestingHelper.StringToSecureString("{>@#F09\0");

            KeyedHashText keyedHash = new KeyedHashText
            {
                Algorithm = enumValue,
                Encoding = new InArgument<Encoding>(ExpressionServices.Convert((env) => System.Text.Encoding.Unicode)),
                KeyInputModeSwitch = KeyInputMode.SecureKey
            };
            Dictionary<string, object> arguments = new Dictionary<string, object>();
            arguments.Add(nameof(KeyedHashText.Input), toHash);
            arguments.Add(nameof(KeyedHashText.KeySecureString), keySecureString);

            WorkflowInvoker invoker = new WorkflowInvoker(keyedHash);
            string activityString = (string)invoker.Invoke(arguments)[nameof(keyedHash.Result)];


            byte[] algorithmBytes = CryptographyHelper.HashDataWithKey(enumValue, Encoding.Unicode.GetBytes(toHash), Encoding.Unicode.GetBytes(TestingHelper.SecureStringToString(keySecureString)));

            Assert.Equal(activityString, BitConverter.ToString(algorithmBytes).Replace("-", string.Empty));
        }

        [Theory]
        [InlineData(SymmetricAlgorithms.AES)]
        [InlineData(SymmetricAlgorithms.AESGCM)]
        [InlineData(SymmetricAlgorithms.DES)]
        [InlineData(SymmetricAlgorithms.RC2)]
        [InlineData(SymmetricAlgorithms.Rijndael)]
        [InlineData(SymmetricAlgorithms.TripleDES)]
        public void SymmetricAlgorithmsEncryptionMatchesWithSecureString(SymmetricAlgorithms enumValue)
        {
            string toProcess = "`~1234567890-=qwertyuiop[]\\ASDFGHJKL:\"ZXCVBNM<>?ăîșțâ";
            SecureString keySecureString = TestingHelper.StringToSecureString("{>@#F09\0");

            EncryptText symmetricAlgorithm = new EncryptText
            {
                Algorithm = enumValue,
                Encoding = new InArgument<Encoding>(ExpressionServices.Convert((env) => System.Text.Encoding.Unicode))
            };
            Dictionary<string, object> arguments = new Dictionary<string, object>();
            arguments.Add(nameof(EncryptText.Input), toProcess);
            arguments.Add(nameof(EncryptText.KeySecureString), keySecureString);

            WorkflowInvoker invoker = new WorkflowInvoker(symmetricAlgorithm);
            string activityString = (string)invoker.Invoke(arguments)[nameof(symmetricAlgorithm.Result)];

            byte[] algorithmBytes = CryptographyHelper.DecryptData(enumValue, Convert.FromBase64String(activityString), Encoding.Unicode.GetBytes(TestingHelper.SecureStringToString(keySecureString)));

            Assert.Equal(toProcess, Encoding.Unicode.GetString(algorithmBytes));
        }

        [Theory]
        [InlineData(SymmetricAlgorithms.AES)]
        [InlineData(SymmetricAlgorithms.AESGCM)]
        [InlineData(SymmetricAlgorithms.DES)]
        [InlineData(SymmetricAlgorithms.RC2)]
        [InlineData(SymmetricAlgorithms.Rijndael)]
        [InlineData(SymmetricAlgorithms.TripleDES)]
        public void SymmetricAlgorithmsDecryptionMatchesWithSecureString(SymmetricAlgorithms enumValue)
        {
            string toProcess = "`~1234567890-=qwertyuiop[]\\ASDFGHJKL:\"ZXCVBNM<>?ăîșțâ";
            SecureString keySecureString = TestingHelper.StringToSecureString("{>@#F09\0");

            byte[] algorithmBytes = CryptographyHelper.EncryptData(enumValue, Encoding.Unicode.GetBytes(toProcess), Encoding.Unicode.GetBytes(TestingHelper.SecureStringToString(keySecureString)));

            DecryptText symmetricAlgorithm = new DecryptText
            {
                Algorithm = enumValue,
                Encoding = new InArgument<Encoding>(ExpressionServices.Convert((env) => System.Text.Encoding.Unicode))
            };

            Dictionary<string, object> arguments = new Dictionary<string, object>();
            arguments.Add(nameof(DecryptText.Input), Convert.ToBase64String(algorithmBytes));
            arguments.Add(nameof(DecryptText.KeySecureString), keySecureString);

            WorkflowInvoker invoker = new WorkflowInvoker(symmetricAlgorithm);
            string activityString = (string)invoker.Invoke(arguments)[nameof(symmetricAlgorithm.Result)];

            Assert.Equal(toProcess, activityString);
        }
    }
}
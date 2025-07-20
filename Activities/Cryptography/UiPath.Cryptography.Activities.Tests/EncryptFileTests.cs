using Shouldly;
using System;
using System.Activities;
using System.Activities.Statements;
using System.IO;
using System.Security;
using UiPath.Cryptography.Enums;
using Xunit;

namespace UiPath.Cryptography.Activities.Tests
{
    public class EncryptFileTests
    {
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void EncryptDecryptFile_HappyPath_Works(bool withOutputOverwrite)
        {
            var tempInputFile = Path.GetTempFileName();
            var tempOutputFile = Path.GetTempFileName();
            var tempOutputFile2 = Path.GetTempFileName();

            try
            {
                // Arrange
                var message = "Hello cryptography world";
                File.WriteAllText(tempInputFile, message);
                if (!withOutputOverwrite)
                {
                    File.Delete(tempOutputFile); // should not exist yet
                    File.Delete(tempOutputFile2); // should not exist yet
                }

                var encryptFile = new EncryptFile
                {
                    InputFilePath = new InArgument<string>(tempInputFile),
                    Key = new InArgument<string>("key"),
                    Algorithm = SymmetricAlgorithms.AESGCM,
                    OutputFilePath = new InArgument<string>(tempOutputFile),
                    KeyInputModeSwitch = KeyInputMode.Key,
                    Overwrite = withOutputOverwrite
                };

                var decryptFile = new DecryptFile
                {
                    InputFilePath = new InArgument<string>(tempOutputFile),
                    Key = new InArgument<string>("key"),
                    Algorithm = SymmetricAlgorithms.AESGCM,
                    OutputFilePath = new InArgument<string>(tempOutputFile2),
                    KeyInputModeSwitch = KeyInputMode.Key,
                    Overwrite = withOutputOverwrite
                };

                var sequence = new Sequence();
                sequence.Activities.Add(encryptFile);
                sequence.Activities.Add(decryptFile);

                // Act
                WorkflowInvoker.Invoke(sequence);

                // Assert
                var outputMessage = File.ReadAllText(tempOutputFile2);
                outputMessage.ShouldBe(message);
            }
            catch (Exception ex)
            {
                Assert.True(false, ex.ToString());
            }
            finally
            {
                // Cleanup
                File.Delete(tempInputFile);
                File.Delete(tempOutputFile);
                File.Delete(tempOutputFile2);
            }
        }

        [Fact]
        public void EncryptFile_WithBothFileAndFilePath_Throws()
        {
            // Arrange
            var encryptFile = new EncryptFile
            {
                InputFile = new InArgument<Platform.ResourceHandling.IResource>(),
                InputFilePath = new InArgument<string>("file"),
                Key = new InArgument<string>("key")
            };

            // Act + Assert
            Should.Throw(() => WorkflowInvoker.Invoke(encryptFile), typeof(ArgumentException));
        }

        [Fact]
        public void EncryptFile_WithoutFileAndFilePath_Throws()
        {
            // Arrange
            var encryptFile = new EncryptFile
            {
                Key = new InArgument<string>("key")
            };

            // Act + Assert
            Should.Throw(() => WorkflowInvoker.Invoke(encryptFile), typeof(ArgumentException));
        }

        [Fact]
        public void EncryptFile_WithBothKeyAndSecureKey_Throws()
        {
            // Arrange
            var secureString = new SecureString();
            secureString.AppendChar('k');

            var encryptFile = new EncryptFile
            {
                InputFilePath = new InArgument<string>("file"),
                Key = new InArgument<string>("key"),
                KeySecureString = new InArgument<System.Security.SecureString>((_) => secureString)
            };

            // Act + Assert
            Should.Throw(() => WorkflowInvoker.Invoke(encryptFile), typeof(ArgumentException));
        }

        [Fact]
        public void EncryptFile_WithoutKeyAndSecureKey_Throws()
        {
            // Arrange
            var encryptFile = new EncryptFile
            {
                InputFilePath = new InArgument<string>("file")
            };

            // Act + Assert
            Should.Throw(() => WorkflowInvoker.Invoke(encryptFile), typeof(ArgumentNullException));
        }
    }
}

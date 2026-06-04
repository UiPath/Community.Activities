using Shouldly;
using System;
using System.Activities;
using System.Activities.Statements;
using System.IO;
using System.Security;
using UiPath.Cryptography.Enums;
using Xunit;

#pragma warning disable CS0618 // tests intentionally set the obsolete *InputModeSwitch properties to exercise legacy behavior.

namespace UiPath.Cryptography.Activities.Tests
{
    public class EncryptFileTests
    {
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void EncryptDecryptFile_HappyPath_Works(bool withOutputOverwrite)
        {
            var tempInputFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            var tempOutputFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            var tempOutputFile2 = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

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
                    Algorithm = EncryptionAlgorithm.AESGCM,
                    OutputFilePath = new InArgument<string>(tempOutputFile),
                    KeyInputModeSwitch = KeyInputMode.Key,
                    Overwrite = withOutputOverwrite
                };

                var decryptFile = new DecryptFile
                {
                    InputFilePath = new InArgument<string>(tempOutputFile),
                    Key = new InArgument<string>("key"),
                    Algorithm = EncryptionAlgorithm.AESGCM,
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
                Assert.Fail(ex.ToString());
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

        [Fact]
        public void EncryptFile_WithOnlyOutputFileName_WritesToInputDirectoryWithThatName()
        {
            // Regression: before the fix, supplying only OutputFileName (with no OutputFilePath)
            // produced an empty filePath that flowed into File.WriteAllBytes("", ...) and threw
            // ArgumentException("Empty path name is not legal"). The fix defaults the directory
            // to the input file's directory.
            var tempInputFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            var inputDir = Path.GetDirectoryName(tempInputFile);
            var customName = "custom-output-" + Guid.NewGuid().ToString("N") + ".enc";
            var expectedOutputPath = Path.Combine(inputDir, customName);

            try
            {
                File.WriteAllText(tempInputFile, "Hello edge-case world");

                var encryptFile = new EncryptFile
                {
                    InputFilePath = new InArgument<string>(tempInputFile),
                    Key = new InArgument<string>("key"),
                    Algorithm = EncryptionAlgorithm.AESGCM,
                    OutputFileName = new InArgument<string>(customName),
                    KeyInputModeSwitch = KeyInputMode.Key,
                };

                WorkflowInvoker.Invoke(encryptFile);

                File.Exists(expectedOutputPath).ShouldBeTrue($"expected encrypted output at {expectedOutputPath}");
                new FileInfo(expectedOutputPath).Length.ShouldBeGreaterThan(0);
            }
            finally
            {
                if (File.Exists(tempInputFile)) File.Delete(tempInputFile);
                if (File.Exists(expectedOutputPath)) File.Delete(expectedOutputPath);
            }
        }
    }
}

#pragma warning restore CS0618

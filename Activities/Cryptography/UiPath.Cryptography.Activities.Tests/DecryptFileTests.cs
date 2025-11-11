using Shouldly;
using System;
using System.Activities;
using System.Security;
using Xunit;

namespace UiPath.Cryptography.Activities.Tests
{
    public class DecryptFileTests
    {
        [Fact]
        public void DecryptFile_WithBothFileAndFilePath_Throws()
        {
            // Arrange
            var decryptFile = new DecryptFile
            {
                InputFile = new InArgument<Platform.ResourceHandling.IResource>(),
                InputFilePath = new InArgument<string>("file"),
                Key = new InArgument<string>("key")
            };

            // Act + Assert
            Should.Throw(() => WorkflowInvoker.Invoke(decryptFile), typeof(ArgumentException));
        }

        [Fact]
        public void DecryptFile_WithoutFileAndFilePath_Throws()
        {
            // Arrange
            var decryptFile = new DecryptFile
            {
                Key = new InArgument<string>("key")
            };

            // Act + Assert
            Should.Throw(() => WorkflowInvoker.Invoke(decryptFile), typeof(ArgumentException));
        }

        [Fact]
        public void DecryptFile_WithBothKeyAndSecureKey_Throws()
        {
            // Arrange
            var secureString = new SecureString();
            secureString.AppendChar('k');

            var decryptFile = new DecryptFile
            {
                InputFilePath = new InArgument<string>("file"),
                Key = new InArgument<string>("key"),
                KeySecureString = new InArgument<System.Security.SecureString>((_) => secureString)
            };

            // Act + Assert
            Should.Throw(() => WorkflowInvoker.Invoke(decryptFile), typeof(ArgumentException));
        }

        [Fact]
        public void DecryptFile_WithoutKeyAndSecureKey_Throws()
        {
            // Arrange
            var decryptFile = new DecryptFile
            {
                InputFilePath = new InArgument<string>("file")
            };

            // Act + Assert
            Should.Throw(() => WorkflowInvoker.Invoke(decryptFile), typeof(ArgumentNullException));
        }
    }
}

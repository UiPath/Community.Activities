using Shouldly;
using System;
using System.Activities;
using System.Security;
using Xunit;

namespace UiPath.Cryptography.Activities.Tests
{
    public class KeyedHashFileTests
    {
        [Fact]
        public void KeyedHashFile_WithBothFileAndFilePath_Throws()
        {
            // Arrange
            var decryptFile = new KeyedHashFile
            {
                InputFile = new InArgument<Platform.ResourceHandling.IResource>(),
                FilePath = new InArgument<string>("file"),
                Key = new InArgument<string>("key")
            };

            // Act + Assert
            Should.Throw(() => WorkflowInvoker.Invoke(decryptFile), typeof(ArgumentException));
        }

        [Fact]
        public void KeyedHashFile_WithoutFileAndFilePath_Throws()
        {
            // Arrange
            var decryptFile = new KeyedHashFile
            {
                Key = new InArgument<string>("key")
            };

            // Act + Assert
            Should.Throw(() => WorkflowInvoker.Invoke(decryptFile), typeof(ArgumentException));
        }

        [Fact]
        public void KeyedHashFile_WithBothKeyAndSecureKey_Throws()
        {
            // Arrange
            var secureString = new SecureString();
            secureString.AppendChar('k');

            var decryptFile = new KeyedHashFile
            {
                FilePath = new InArgument<string>("file"),
                Key = new InArgument<string>("key"),
                KeySecureString = new InArgument<System.Security.SecureString>((_) => secureString)
            };

            // Act + Assert
            Should.Throw(() => WorkflowInvoker.Invoke(decryptFile), typeof(ArgumentException));
        }

        [Fact]
        public void KeyedHashFile_WithoutKeyAndSecureKey_Throws()
        {
            // Arrange
            var decryptFile = new KeyedHashFile
            {
                FilePath = new InArgument<string>("file")
            };

            // Act + Assert
            Should.Throw(() => WorkflowInvoker.Invoke(decryptFile), typeof(ArgumentNullException));
        }
    }
}

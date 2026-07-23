using System.IO;
using Moq;
using UiPath.Cryptography.Activities.Helpers;
using UiPath.Platform.ResourceHandling;
using Xunit;

namespace UiPath.Cryptography.Activities.Tests.Helpers
{
    /// <summary>
    /// Covers the path↔resource precedence of <see cref="PgpFileResolver.ResolveLocalPath"/> — the
    /// shared resolver used by the synchronous Encrypt/Decrypt Text &amp; File activities for the PGP
    /// public/private key inputs. The string path wins; the resource is only consulted when the path
    /// is empty or whitespace. The resource-fallback branch is exercised end-to-end with a real
    /// <see cref="LocalResource"/> so a regression in the conversion/resolution path fails a test.
    /// </summary>
    public class PgpFileResolverTests
    {
        [Fact]
        public void ResolveLocalPath_PathProvided_ReturnsPath_AndDoesNotTouchResource()
        {
            // Strict mock: any access to the resource would throw, proving the path short-circuits it.
            var resource = new Mock<IResource>(MockBehavior.Strict);

            var result = PgpFileResolver.ResolveLocalPath(@"C:\keys\key.asc", resource.Object);

            Assert.Equal(@"C:\keys\key.asc", result);
            resource.VerifyNoOtherCalls();
        }

        [Fact]
        public void ResolveLocalPath_PathProvided_NullResource_ReturnsPath()
        {
            var result = PgpFileResolver.ResolveLocalPath(@"C:\keys\key.asc", null);

            Assert.Equal(@"C:\keys\key.asc", result);
        }

        [Fact]
        public void ResolveLocalPath_EmptyPath_NullResource_ReturnsEmpty()
        {
            Assert.Equal(string.Empty, PgpFileResolver.ResolveLocalPath(string.Empty, null));
        }

        [Fact]
        public void ResolveLocalPath_NullPath_NullResource_ReturnsNull()
        {
            Assert.Null(PgpFileResolver.ResolveLocalPath(null, null));
        }

        [Fact]
        public void ResolveLocalPath_EmptyPath_WithResource_ResolvesToResourceLocalPath()
        {
            // Positive coverage for the resource-fallback branch: with an empty path the resolver must
            // convert the IResource to a local path via ToLocalResource/ResolveAsync. A real
            // LocalResource exercises the actual conversion path (not just the precedence logic).
            var tempFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            File.WriteAllText(tempFile, "key");
            try
            {
                IResource resource = LocalResource.FromPath(tempFile);

                var result = PgpFileResolver.ResolveLocalPath(string.Empty, resource);

                Assert.False(string.IsNullOrEmpty(result));
                Assert.Equal(Path.GetFullPath(tempFile), Path.GetFullPath(result));
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void ResolveLocalPath_WhitespacePath_WithResource_FallsBackToResource()
        {
            // A whitespace-only path must not win over a bound resource — the resolver treats it as
            // empty (IsNullOrWhiteSpace), matching the async ResolveAsync, and falls back to the resource.
            var tempFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            File.WriteAllText(tempFile, "key");
            try
            {
                IResource resource = LocalResource.FromPath(tempFile);

                var result = PgpFileResolver.ResolveLocalPath("   ", resource);

                Assert.Equal(Path.GetFullPath(tempFile), Path.GetFullPath(result));
            }
            finally
            {
                File.Delete(tempFile);
            }
        }
    }
}

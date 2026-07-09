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
    /// is empty (the resource-resolution adapter itself is exercised end-to-end by the PGP activity
    /// tests, not here, since ToLocalResource needs the platform resource stack).
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
    }
}

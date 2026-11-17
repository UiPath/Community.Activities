using UiPath.Java.Activities;
using Xunit;

namespace UiPath.Java.Activities.API.Tests
{
    public class PathResolverTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void ToFullPath_NullEmptyOrWhitespace_ReturnedUnchanged(string path)
        {
            Assert.Equal(path, PathResolver.ToFullPath(path));
        }

        [Theory]
        [InlineData("lib\\x.jar")]
        [InlineData("lib/x.jar")]
        public void ToFullPath_RelativePath_ResolvedAgainstCurrentDirectoryWithPlatformSeparators(string path)
        {
            var expected = Path.Combine(Directory.GetCurrentDirectory(), "lib", "x.jar");

            Assert.Equal(expected, PathResolver.ToFullPath(path));
        }

        // CI runs on Windows, where GetFullPath accepts both separators; pin the separator step itself,
        // with the Linux/macOS separator, so removing the Replace calls fails the test.
        [Theory]
        [InlineData("lib\\x.jar", "lib/x.jar")]
        [InlineData("lib/sub\\x.jar", "lib/sub/x.jar")]
        [InlineData("x.jar", "x.jar")]
        public void NormalizeSeparators_RelativePath_UsesTargetSeparator(string path, string expected)
        {
            Assert.Equal(expected, PathResolver.NormalizeSeparators(path, '/'));
        }

        [Fact]
        public void NormalizeSeparators_RootedPath_Unchanged()
        {
            Assert.Equal("/opt/libs\\x.jar", PathResolver.NormalizeSeparators("/opt/libs\\x.jar", '/'));
        }

        [Fact]
        public void ToFullPath_AbsolutePath_Unchanged()
        {
            var absolute = Path.Combine(Path.GetTempPath(), "libs", "x.jar");

            Assert.Equal(absolute, PathResolver.ToFullPath(absolute));
        }
    }
}

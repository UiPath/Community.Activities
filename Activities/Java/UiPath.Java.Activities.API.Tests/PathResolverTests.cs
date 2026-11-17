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

        [Fact]
        public void ToFullPath_AbsolutePath_Unchanged()
        {
            var absolute = Path.Combine(Path.GetTempPath(), "libs", "x.jar");

            Assert.Equal(absolute, PathResolver.ToFullPath(absolute));
        }
    }
}

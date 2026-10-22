using System;
using Xunit;

namespace UiPath.Python.Tests
{
    public class VersionExtensionsTests
    {
        // These two tests are intentionally coupled.
        // If Version_HasExactlyTheExpectedEnumValues fails, a Version enum value was added or removed.
        // Review whether GetSupportedVersion in VersionExtensions should also be updated,
        // then update the expected array in both tests to reflect the new state.

        [Fact]
        public void Version_HasExactlyTheExpectedEnumValues()
        {
            var expected = new[]
            {
                Version.Auto,
                Version.Python_27,
                Version.Python_33,
                Version.Python_34,
                Version.Python_35,
                Version.Python_36,
                Version.Python_37,
                Version.Python_38,
                Version.Python_39,
                Version.Python_310,
            };

            Assert.Equal(expected, Enum.GetValues<Version>());
        }

        [Fact]
        public void GetSupportedVersion_ReturnsExactlyTheExpectedVersions()
        {
            // If this test fails, update the expected array to match the intended supported set.
            // Also check Version_HasExactlyTheExpectedEnumValues to keep both in sync.
            var expected = new[]
            {
                Version.Auto,
                Version.Python_310,
            };

            // GetSupportedVersions is obsolete but retained for backward compatibility; assert it
            // still returns the documented value.
#pragma warning disable CS0618 // Type or member is obsolete
            Assert.Equal(expected, VersionExtensions.GetSupportedVersions());
#pragma warning restore CS0618
        }

        [Theory]
        [InlineData(3, 10, true)]
        [InlineData(3, 13, true)]
        [InlineData(3, 14, true)]
        [InlineData(3, 15, true)]
        [InlineData(3, 9, false)]
        [InlineData(3, 16, false)]
        [InlineData(2, 7, false)]
        public void IsRuntimeVersionSupported_MatchesTheSupportedRange(int major, int minor, bool expected)
        {
            // Supported runtimes are 3.10 through 3.15; bump this together with the list in Version.cs.
            Assert.Equal(expected, VersionExtensions.IsRuntimeVersionSupported(major, minor));
        }

        [Fact]
        public void SupportedRuntimeVersionsDisplay_ListsEverySupportedVersion()
        {
            Assert.Equal("3.10, 3.11, 3.12, 3.13, 3.14, 3.15", VersionExtensions.GetSupportedRuntimeVersionsDisplay());
        }
    }
}

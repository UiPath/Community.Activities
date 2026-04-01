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
                Version.Python_36,
                Version.Python_37,
                Version.Python_38,
                Version.Python_39,
                Version.Python_310,
            };

            Assert.Equal(expected, VersionExtensions.GetSupportedVersion());
        }
    }
}

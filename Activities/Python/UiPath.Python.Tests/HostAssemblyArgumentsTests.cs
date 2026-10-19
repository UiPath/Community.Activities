using System.Collections.Generic;
using System.Linq;
using UiPath.Shared.Service;
using UiPath.TestUtils;
using Xunit;
using Assert = Xunit.Assert;

namespace UiPath.Python.Tests
{
    // The client formats the assembly map into command-line arguments and the host parses it back.
    public class HostAssemblyArgumentsTests
    {
        private const string Category = "Python";

        [Fact]
        [Trait(TestCategories.Category, Category)]
        public void FormatThenParse_RoundTrips_PathsWithSpacesAndSeparator()
        {
            var assemblies = new Dictionary<string, string>
            {
                ["Python.Runtime"] = @"C:\Program Files\pkgs\pythonnet\Python.Runtime.dll",
                ["System.Xaml"] = "/opt/a=b/System.Xaml.dll",
            };

            var parsed = HostAssemblyArguments.TryParse(HostAssemblyArguments.Format(assemblies).ToList(), out var result, out var error);

            Assert.True(parsed, error);
            Assert.Equal(assemblies, result);
        }

        [Fact]
        [Trait(TestCategories.Category, Category)]
        public void Format_NullMap_ProducesNoArguments()
        {
            Assert.Empty(HostAssemblyArguments.Format(null));
        }

        [Fact]
        [Trait(TestCategories.Category, Category)]
        public void TryParse_NoArguments_IsValidAndEmpty()
        {
            Assert.True(HostAssemblyArguments.TryParse(new string[0], out var result, out _));
            Assert.Empty(result);
        }

        [Fact]
        [Trait(TestCategories.Category, Category)]
        public void TryParse_IsCaseInsensitiveOnNames()
        {
            HostAssemblyArguments.TryParse(new[] { @"python.runtime=C:\x\Python.Runtime.dll" }, out var result, out _);

            Assert.True(result.ContainsKey("Python.Runtime"));
        }

        [Theory]
        [Trait(TestCategories.Category, Category)]
        [InlineData("NoSeparator")]
        [InlineData("=C:/x.dll")]
        [InlineData("Name=")]
        public void TryParse_MalformedArgument_Fails(string argument)
        {
            Assert.False(HostAssemblyArguments.TryParse(new[] { argument }, out _, out var error));
            Assert.Contains(argument, error);
        }

        [Fact]
        [Trait(TestCategories.Category, Category)]
        public void TryParse_DuplicateName_Fails()
        {
            Assert.False(HostAssemblyArguments.TryParse(new[] { "A=1", "a=2" }, out _, out var error));
            Assert.Contains("Duplicate", error);
        }
    }
}

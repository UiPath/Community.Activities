using UiPath.Activities.Java.ViewModels;
using Xunit;

namespace UiPath.Java.Activities.API.Tests
{
    public class LocalResourcePathNormalizerTests
    {
        // written as concatenations so the backslash escapes stay readable
        private const string B = "\\";
        private const string Q = "\"";

        public static TheoryData<string, string> PickerValues => new()
        {
            { $"LocalResource.FromPath({Q}C:{B}{B}libs{B}{B}x.jar{Q})", $"{Q}C:{B}libs{B}x.jar{Q}" },
            { $"LocalResource.FromPath({Q}lib{B}{B}x.jar{Q})", $"{Q}lib/x.jar{Q}" },
            { $"{Q}lib{B}x.jar{Q}", $"{Q}lib/x.jar{Q}" },
        };

        [Theory]
        [MemberData(nameof(PickerValues))]
        public void Normalize_RewritesPickerValues(string input, string expected)
        {
            Assert.Equal(expected, LocalResourcePathNormalizer.Normalize(input));
        }

        public static TheoryData<string> UntouchedValues => new()
        {
            $"{Q}C:{B}libs{B}x.jar{Q}",
            $"C:{B}libs{B}x.jar",
            "/opt/libs/x.jar",
            $"jarFolder + {Q}{B}x.jar{Q}",
            "myJarPath",
            $"lib{B}x.jar", // unquoted: could be an expression, resolved at runtime instead
            "",
        };

        [Theory]
        [MemberData(nameof(UntouchedValues))]
        public void Normalize_AbsolutePathsAndExpressions_Unchanged(string input)
        {
            Assert.Equal(input, LocalResourcePathNormalizer.Normalize(input));
        }

        [Fact]
        public void Normalize_NonString_Unchanged()
        {
            var value = new object();
            Assert.Same(value, LocalResourcePathNormalizer.Normalize(value));
            Assert.Null(LocalResourcePathNormalizer.Normalize(null));
        }
    }
}

using System.Activities;
using System.Activities.Expressions;
using Microsoft.VisualBasic.Activities;
using UiPath.Shared.ViewModels.Helpers;
using Xunit;

namespace UiPath.Shared.ViewModels.Tests
{
    /// <summary>
    /// Older Studio versions report LocalResource as supported but ignore NoWrap, so the picker would store
    /// LocalResource.FromPath("...") in a string argument. The ViewModels of the packs that adopt the picker unwrap it again (Java, FTP, Python).
    /// </summary>
    public class LocalResourcePathNormalizerTests
    {
        // written as constants so the backslash escapes stay readable
        private const string B = "\\";
        private const string Q = "\"";

        public static TheoryData<string, string> PickerValues => new()
        {
            { $"LocalResource.FromPath({Q}C:{B}{B}files{B}{B}a.txt{Q})", $"{Q}C:{B}files{B}a.txt{Q}" },
            { $"LocalResource.FromPath({Q}files{B}{B}a.txt{Q})", $"{Q}files/a.txt{Q}" },
            { $"{Q}files{B}a.txt{Q}", $"{Q}files/a.txt{Q}" },
        };

        [Theory]
        [MemberData(nameof(PickerValues))]
        public void NormalizeText_RewritesPickerValues(string input, string expected)
        {
            Assert.Equal(expected, LocalResourcePathNormalizer.NormalizeText(input));
        }

        public static TheoryData<string> UntouchedValues => new()
        {
            $"{Q}C:{B}files{B}a.txt{Q}",
            $"C:{B}files{B}a.txt",
            "/opt/files/a.txt",
            $"folder + {Q}{B}a.txt{Q}",
            "myLocalPath",
            $"files{B}a.txt", // unquoted: could be an expression, resolved at runtime instead
            "",
        };

        [Theory]
        [MemberData(nameof(UntouchedValues))]
        public void NormalizeText_AbsolutePathsAndExpressions_Unchanged(string input)
        {
            Assert.Equal(input, LocalResourcePathNormalizer.NormalizeText(input));
        }

        private static InArgument<string> FromPathExpression(string path) =>
            new(new VisualBasicValue<string>($"LocalResource.FromPath({Q}{path}{Q})"));

        private static string LiteralValue(object value) =>
            Assert.IsType<Literal<string>>(Assert.IsType<InArgument<string>>(value).Expression).Value;

        [Fact]
        public void Normalize_Expression_SameInstance()
        {
            var argument = new InArgument<string>(new VisualBasicValue<string>("folder & \"/a.txt\""));

            Assert.Same(argument, LocalResourcePathNormalizer.Normalize(argument));
        }

        [Fact]
        public void Normalize_AbsoluteLiteral_SameInstance()
        {
            var argument = new InArgument<string>(new Literal<string>($"C:{B}files{B}a.txt"));

            Assert.Same(argument, LocalResourcePathNormalizer.Normalize(argument));
        }

        [Fact]
        public void Normalize_NotAnInArgument_Unchanged()
        {
            var value = new object();
            Assert.Same(value, LocalResourcePathNormalizer.Normalize(value));
            Assert.Null(LocalResourcePathNormalizer.Normalize(null));
        }
    }
}

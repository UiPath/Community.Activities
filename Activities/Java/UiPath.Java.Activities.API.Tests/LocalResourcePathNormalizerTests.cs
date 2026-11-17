using System.Activities;
using System.Activities.Expressions;
using System.Activities.DesignViewModels;
using Microsoft.VisualBasic.Activities;
using UiPath.Activities.Java.ViewModels;
using Xunit;

namespace UiPath.Java.Activities.API.Tests
{
    public class LocalResourcePathNormalizerTests
    {
        // written as constants so the backslash escapes stay readable
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
        public void NormalizeText_RewritesPickerValues(string input, string expected)
        {
            Assert.Equal(expected, LocalResourcePathNormalizer.NormalizeText(input));
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
        public void NormalizeText_AbsolutePathsAndExpressions_Unchanged(string input)
        {
            Assert.Equal(input, LocalResourcePathNormalizer.NormalizeText(input));
        }

        [Fact]
        public void NormalizeText_Null_ReturnsNull()
        {
            Assert.Null(LocalResourcePathNormalizer.NormalizeText(null));
        }

        // What Studio actually hands to UpdateAsync: an InArgument<string>.

        private static InArgument<string> FromPathExpression(string path) =>
            new(new VisualBasicValue<string>($"LocalResource.FromPath({Q}{path}{Q})"));

        private static string LiteralValue(object value) =>
            Assert.IsType<Literal<string>>(Assert.IsType<InArgument<string>>(value).Expression).Value;

        [Fact]
        public void Normalize_FromPathExpression_BecomesStringLiteral()
        {
            var result = LocalResourcePathNormalizer.Normalize(FromPathExpression($"C:{B}{B}libs{B}{B}x.jar"));

            Assert.Equal($"C:{B}libs{B}x.jar", LiteralValue(result));
        }

        [Fact]
        public void Normalize_RelativeFromPathExpression_BecomesLiteralWithForwardSlashes()
        {
            var result = LocalResourcePathNormalizer.Normalize(FromPathExpression($"lib{B}{B}x.jar"));

            Assert.Equal("lib/x.jar", LiteralValue(result));
        }

        [Fact]
        public void Normalize_RelativeLiteral_UsesForwardSlashes()
        {
            var result = LocalResourcePathNormalizer.Normalize(new InArgument<string>(new Literal<string>($"lib{B}x.jar")));

            Assert.Equal("lib/x.jar", LiteralValue(result));
        }

        [Fact]
        public void Normalize_AbsoluteLiteral_SameInstance()
        {
            var argument = new InArgument<string>(new Literal<string>($"C:{B}libs{B}x.jar"));

            Assert.Same(argument, LocalResourcePathNormalizer.Normalize(argument));
        }

        [Fact]
        public void Normalize_Expression_SameInstance()
        {
            var argument = new InArgument<string>(new VisualBasicValue<string>("jarFolder & \"/x.jar\""));

            Assert.Same(argument, LocalResourcePathNormalizer.Normalize(argument));
        }

        [Fact]
        public void Normalize_NotAnInArgument_Unchanged()
        {
            var value = new object();
            Assert.Same(value, LocalResourcePathNormalizer.Normalize(value));
            Assert.Null(LocalResourcePathNormalizer.Normalize(null));
        }

        [Fact]
        public async Task LoadJarViewModel_UpdateAsync_UnwrapsFromPathIntoStringLiteral()
        {
            var vm = new LoadJarViewModel(Moq.Mock.Of<IDesignServices>());
            await vm.InitializeAsync();

            await vm.UpdateAsync("JarPath", FromPathExpression($"lib{B}{B}x.jar"));

            Assert.Equal("lib/x.jar", LiteralValue(vm.JarPath.Value));
        }
    }
}

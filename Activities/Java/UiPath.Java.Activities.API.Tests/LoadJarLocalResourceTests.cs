using System.Activities;
using System.Activities.Expressions;
using System.Activities.DesignViewModels;
using Microsoft.VisualBasic.Activities;
using UiPath.Activities.Java.ViewModels;
using Xunit;

namespace UiPath.Java.Activities.API.Tests
{
    public class LoadJarLocalResourceTests
    {
        // written as constants so the backslash escapes stay readable
        private const string B = "\\";
        private const string Q = "\"";

        // What Studio actually hands to UpdateAsync: an InArgument<string>.

        private static InArgument<string> FromPathExpression(string path) =>
            new(new VisualBasicValue<string>($"LocalResource.FromPath({Q}{path}{Q})"));

        private static string LiteralValue(object value) =>
            Assert.IsType<Literal<string>>(Assert.IsType<InArgument<string>>(value).Expression).Value;

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

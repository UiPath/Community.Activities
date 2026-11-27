using System.Activities;
using System.Activities.DesignViewModels;
using System.Activities.Expressions;
using System.Activities.ViewModels;
using System.Activities.ViewModels.Interfaces;
using System.Threading.Tasks;
using Microsoft.VisualBasic.Activities;
using Moq;
using UiPath.Activities.Python.ViewModels;
using Xunit;

namespace UiPath.Python.Activities.Tests
{
    /// <summary>
    /// Older Studio versions ignore NoWrap: the path ViewModels unwrap LocalResource.FromPath("...") on update
    /// (the normalizer itself is shared and tested in UiPath.Shared.ViewModels.Tests).
    /// </summary>
    public class LocalResourcePathViewModelTests
    {
        private const string B = "\\";
        private const string Q = "\"";

        private static InArgument<string> FromPathExpression(string path) =>
            new(new VisualBasicValue<string>($"LocalResource.FromPath({Q}{path}{Q})"));

        private static string LiteralValue(object value) =>
            Assert.IsType<Literal<string>>(Assert.IsType<InArgument<string>>(value).Expression).Value;

        [Theory]
        [InlineData(typeof(LoadScriptViewModel), "ScriptFile")]
        [InlineData(typeof(RunScriptViewModel), "ScriptFile")]
        [InlineData(typeof(PythonScopeViewModel), "Path")]
        [InlineData(typeof(PythonScopeViewModel), "LibraryPath")]
        [InlineData(typeof(PythonScopeViewModel), "WorkingFolder")]
        public async Task UpdateAsync_UnwrapsFromPathIntoStringLiteral(System.Type viewModelType, string propertyName)
        {
            var vm = (DesignPropertiesViewModel)System.Activator.CreateInstance(viewModelType, Mock.Of<IDesignServices>());
            await vm.InitializeAsync();

            await vm.UpdateAsync(propertyName, FromPathExpression($"scripts{B}{B}a.py"));

            var stored = (DesignInArgument<string>)viewModelType.GetProperty(propertyName).GetValue(vm);
            Assert.Equal("scripts/a.py", LiteralValue(stored.Value));
        }
    }
}

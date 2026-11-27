using System.Activities;
using System.Activities.DesignViewModels;
using System.Activities.Expressions;
using System.Activities.ViewModels;
using System.Activities.ViewModels.Interfaces;
using System.Threading.Tasks;
using Microsoft.VisualBasic.Activities;
using Moq;
using UiPath.FTP.Activities.NetCore.ViewModels;
using Xunit;

namespace UiPath.FTP.Tests
{
    /// <summary>
    /// Older Studio versions ignore NoWrap: the local path ViewModels unwrap LocalResource.FromPath("...") on update
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
        [InlineData(typeof(DownloadFilesViewModel), "LocalPath")]
        [InlineData(typeof(UploadFilesViewModel), "LocalPath")]
        [InlineData(typeof(WithFtpSessionViewModel), "ClientCertificatePath")]
        public async Task UpdateAsync_UnwrapsFromPathIntoStringLiteral(System.Type viewModelType, string propertyName)
        {
            var vm = (DesignPropertiesViewModel)System.Activator.CreateInstance(viewModelType, Mock.Of<IDesignServices>());
            await vm.InitializeAsync();

            await vm.UpdateAsync(propertyName, FromPathExpression($"files{B}{B}a.txt"));

            var stored = (DesignInArgument<string>)viewModelType.GetProperty(propertyName).GetValue(vm);
            Assert.Equal("files/a.txt", LiteralValue(stored.Value));
        }

        [Fact]
        public async Task UpdateAsync_RemotePath_IsNotNormalized()
        {
            var vm = new DownloadFilesViewModel(Mock.Of<IDesignServices>());
            await vm.InitializeAsync();
            var argument = FromPathExpression($"files{B}{B}a.txt");

            await vm.UpdateAsync("RemotePath", argument);

            Assert.Same(argument, vm.RemotePath.Value);
        }
    }
}

using System.Activities;
using System.Activities.DesignViewModels;
using System.Activities.Expressions;
using Microsoft.VisualBasic.Activities;
using Moq;
using Shouldly;
using UiPath.Cryptography;
using UiPath.Cryptography.Activities.NetCore.ViewModels;
using Xunit;

namespace UiPath.Cryptography.Activities.Tests
{
    /// <summary>
    /// Guards the contract of <c>FormatChanged_Action</c> on the symmetric Encrypt/Decrypt ViewModels:
    /// switching the <c>Format</c> dropdown snaps an untouched/literal <c>KdfIterations</c> to the new
    /// format's recommended iteration count (helpful default), but must NOT overwrite a user-bound
    /// VB/argument expression — doing so silently loses design-time data and lets the encrypt and decrypt
    /// sides drift to different iteration counts (STUD-80533).
    /// </summary>
    public class FormatChangeKdfIterationsTests
    {
        private static EncryptTextViewModel NewEncryptViewModel() =>
            new EncryptTextViewModel(Mock.Of<IDesignServices>());

        private static DecryptTextViewModel NewDecryptViewModel() =>
            new DecryptTextViewModel(Mock.Of<IDesignServices>());

        [Fact]
        public void Encrypt_FormatChange_PreservesBoundKdfIterationsExpression()
        {
            var vm = NewEncryptViewModel();
            vm.Format.Value = SymmetricWireFormat.Owasp2026;
            var boundExpression = new VisualBasicValue<int>("In_KdfIterations");
            vm.KdfIterations.Value = new InArgument<int> { Expression = boundExpression };

            vm.Format.Value = SymmetricWireFormat.OpenSslEnc;
            vm.FormatChanged_Action();

            vm.KdfIterations.Value.Expression.ShouldBeSameAs(boundExpression);
            ((VisualBasicValue<int>)vm.KdfIterations.Value.Expression).ExpressionText.ShouldBe("In_KdfIterations");
        }

        [Fact]
        public void Decrypt_FormatChange_PreservesBoundKdfIterationsExpression()
        {
            var vm = NewDecryptViewModel();
            vm.Format.Value = SymmetricWireFormat.Owasp2026;
            var boundExpression = new VisualBasicValue<int>("In_KdfIterations");
            vm.KdfIterations.Value = new InArgument<int> { Expression = boundExpression };

            vm.Format.Value = SymmetricWireFormat.OpenSslEnc;
            vm.FormatChanged_Action();

            vm.KdfIterations.Value.Expression.ShouldBeSameAs(boundExpression);
            ((VisualBasicValue<int>)vm.KdfIterations.Value.Expression).ExpressionText.ShouldBe("In_KdfIterations");
        }

        [Fact]
        public void Encrypt_FormatChange_SnapsLiteralKdfIterationsToRecommendedValue()
        {
            var vm = NewEncryptViewModel();
            vm.Format.Value = SymmetricWireFormat.OpenSslEnc;
            vm.KdfIterations.Value = new InArgument<int>(50_000); // a Literal<int>, not a binding

            vm.Format.Value = SymmetricWireFormat.Owasp2026;
            vm.FormatChanged_Action();

            var literal = vm.KdfIterations.Value.Expression.ShouldBeOfType<Literal<int>>();
            literal.Value.ShouldBe(1_300_000); // Owasp2026 recommended PBKDF2-HMAC-SHA1 count
        }

        [Fact]
        public void Decrypt_FormatChange_SnapsLiteralKdfIterationsToRecommendedValue()
        {
            var vm = NewDecryptViewModel();
            vm.Format.Value = SymmetricWireFormat.OpenSslEnc;
            vm.KdfIterations.Value = new InArgument<int>(50_000); // a Literal<int>, not a binding

            vm.Format.Value = SymmetricWireFormat.Owasp2026;
            vm.FormatChanged_Action();

            var literal = vm.KdfIterations.Value.Expression.ShouldBeOfType<Literal<int>>();
            literal.Value.ShouldBe(1_300_000); // Owasp2026 recommended PBKDF2-HMAC-SHA1 count
        }
    }
}

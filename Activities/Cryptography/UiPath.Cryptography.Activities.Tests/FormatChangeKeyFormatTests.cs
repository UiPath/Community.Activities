using System.Activities.DesignViewModels;
using Moq;
using Shouldly;
using UiPath.Cryptography;
using UiPath.Cryptography.Activities.NetCore.ViewModels;
using Xunit;

namespace UiPath.Cryptography.Activities.Tests
{
    /// <summary>
    /// Guards the contract of <c>FormatChanged_Action</c> on the symmetric Encrypt/Decrypt ViewModels
    /// for the <c>KeyFormat</c> dropdown. The action is wired as a Format rule, so it ALSO fires on reload
    /// when the persisted Format value is applied to the design property. It therefore must preserve a
    /// user-selected Hex/Base64 raw-key format and only correct the Encoded default (which Raw's dropdown
    /// doesn't offer) up to Hex — overwriting it unconditionally dropped a saved Base64 selection back to
    /// Hex on every reopen (STUD-80535).
    /// </summary>
    public class FormatChangeKeyFormatTests
    {
        private static EncryptTextViewModel NewEncryptViewModel() =>
            new EncryptTextViewModel(Mock.Of<IDesignServices>());

        private static DecryptTextViewModel NewDecryptViewModel() =>
            new DecryptTextViewModel(Mock.Of<IDesignServices>());

        [Fact]
        public void Encrypt_RawReload_PreservesSelectedBase64KeyFormat()
        {
            var vm = NewEncryptViewModel();
            // User selects Raw, then picks Base64 over the Hex default.
            vm.Format.Value = SymmetricWireFormat.Raw;
            vm.FormatChanged_Action();
            vm.KeyFormat.Value = KeyBytesFormat.Base64;

            // Reopening the XAML re-applies the persisted Format value, firing the rule again.
            vm.FormatChanged_Action();

            vm.KeyFormat.Value.ShouldBe(KeyBytesFormat.Base64);
        }

        [Fact]
        public void Decrypt_RawReload_PreservesSelectedBase64KeyFormat()
        {
            var vm = NewDecryptViewModel();
            vm.Format.Value = SymmetricWireFormat.Raw;
            vm.FormatChanged_Action();
            vm.KeyFormat.Value = KeyBytesFormat.Base64;

            vm.FormatChanged_Action();

            vm.KeyFormat.Value.ShouldBe(KeyBytesFormat.Base64);
        }

        [Fact]
        public void Encrypt_FormatChangeToRaw_SnapsEncodedDefaultToHex()
        {
            var vm = NewEncryptViewModel();
            vm.KeyFormat.Value = KeyBytesFormat.Encoded;

            vm.Format.Value = SymmetricWireFormat.Raw;
            vm.FormatChanged_Action();

            vm.KeyFormat.Value.ShouldBe(KeyBytesFormat.Hex);
        }

        [Fact]
        public void Decrypt_FormatChangeToRaw_SnapsEncodedDefaultToHex()
        {
            var vm = NewDecryptViewModel();
            vm.KeyFormat.Value = KeyBytesFormat.Encoded;

            vm.Format.Value = SymmetricWireFormat.Raw;
            vm.FormatChanged_Action();

            vm.KeyFormat.Value.ShouldBe(KeyBytesFormat.Hex);
        }

        [Fact]
        public void Encrypt_FormatChangeToNonRaw_ForcesEncoded()
        {
            var vm = NewEncryptViewModel();
            vm.Format.Value = SymmetricWireFormat.Raw;
            vm.FormatChanged_Action();
            vm.KeyFormat.Value = KeyBytesFormat.Base64;

            // Leaving Raw: the field is hidden and runtime rejects Hex/Base64 for non-Raw formats.
            vm.Format.Value = SymmetricWireFormat.Owasp2026;
            vm.FormatChanged_Action();

            vm.KeyFormat.Value.ShouldBe(KeyBytesFormat.Encoded);
        }

        [Fact]
        public void Decrypt_FormatChangeToNonRaw_ForcesEncoded()
        {
            var vm = NewDecryptViewModel();
            vm.Format.Value = SymmetricWireFormat.Raw;
            vm.FormatChanged_Action();
            vm.KeyFormat.Value = KeyBytesFormat.Base64;

            vm.Format.Value = SymmetricWireFormat.Owasp2026;
            vm.FormatChanged_Action();

            vm.KeyFormat.Value.ShouldBe(KeyBytesFormat.Encoded);
        }
    }
}

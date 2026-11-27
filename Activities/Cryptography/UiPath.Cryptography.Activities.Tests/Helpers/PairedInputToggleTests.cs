using System.Activities;
using System.Activities.DesignViewModels;
using System.Security;
using UiPath.Cryptography.Activities.Helpers;
using UiPath.Platform.ResourceHandling;
using Xunit;

namespace UiPath.Cryptography.Activities.Tests.Helpers
{
    /// <summary>
    /// Exercises the persisted-state contract that <see cref="PairedInputToggle{TPrimary, TSecondary}"/>
    /// implements: when a workflow with one side bound and the other empty is reopened,
    /// <c>UseSecondary</c> must reflect which side had the persisted value. This is what drives
    /// the visibility flags applied by host ViewModels (Encrypt/Decrypt/Hash/PgpSign) when a saved
    /// workflow is loaded — viogroza's "persisted-PGP-mode open" review item.
    /// </summary>
    public class PairedInputToggleTests
    {
        [Fact]
        public void BothEmpty_OnOpen_PrefersPrimary()
        {
            var primary = new DesignInArgument<string>();
            var secondary = new DesignInArgument<SecureString>();

            var toggle = new PairedInputToggle<string, SecureString>(
                primary, secondary, "Use primary", "Use secondary");

            toggle.ConfigureMenuActions();

            Assert.False(toggle.UseSecondary);
        }

        [Fact]
        public void OnlyPrimaryPersisted_OnOpen_KeepsPrimary()
        {
            var primary = new DesignInArgument<string> { Value = new InArgument<string>("hello") };
            var secondary = new DesignInArgument<SecureString>();

            var toggle = new PairedInputToggle<string, SecureString>(
                primary, secondary, "Use primary", "Use secondary");

            toggle.ConfigureMenuActions();

            Assert.False(toggle.UseSecondary);
        }

        [Fact]
        public void OnlySecondaryPersisted_OnOpen_FlipsToSecondary()
        {
            // Mirrors a workflow saved with PassphraseSecureString bound but Passphrase empty:
            // on re-open the VM must surface the SecureString field, not the string one.
            var primary = new DesignInArgument<string>();
            var secondary = new DesignInArgument<SecureString> { Value = new InArgument<SecureString>() };

            var toggle = new PairedInputToggle<string, SecureString>(
                primary, secondary, "Use primary", "Use secondary");

            toggle.ConfigureMenuActions();

            Assert.True(toggle.UseSecondary);
        }

        [Fact]
        public void BothPersisted_OnOpen_PrefersPrimary()
        {
            // Edge case: defensive — if both sides somehow have a value, the primary wins.
            // Documents the implementation choice so a future change here is intentional.
            var primary = new DesignInArgument<string> { Value = new InArgument<string>("hello") };
            var secondary = new DesignInArgument<SecureString> { Value = new InArgument<SecureString>() };

            var toggle = new PairedInputToggle<string, SecureString>(
                primary, secondary, "Use primary", "Use secondary");

            toggle.ConfigureMenuActions();

            Assert.False(toggle.UseSecondary);
        }

        [Fact]
        public void PublicKeyFile_IResourcePersisted_OnOpen_FlipsToSecondary()
        {
            // Same shape as the symmetric/passphrase toggle but with the path/IResource pair
            // used by EncryptCryptoViewModelBase.PublicKeyFile (and PgpSign/PgpVerify equivalents).
            var primary = new DesignInArgument<string>();
            var secondary = new DesignInArgument<IResource> { Value = new InArgument<IResource>() };

            var toggle = new PairedInputToggle<string, IResource>(
                primary, secondary, "Use file path", "Use file");

            toggle.ConfigureMenuActions();

            Assert.True(toggle.UseSecondary);
        }

    }
}

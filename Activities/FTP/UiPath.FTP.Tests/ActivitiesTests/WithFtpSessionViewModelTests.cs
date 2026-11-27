using System.Activities.DesignViewModels;
using System.Threading.Tasks;
using Moq;
using UiPath.FTP.Activities.NetCore.ViewModels;
using UiPath.FTP.Enums;
using Xunit;

namespace UiPath.FTP.Tests
{
    /// <summary>
    /// Coverage for the unified-canvas migration's additions to
    /// <see cref="WithFtpSessionViewModel"/>: <c>InitializeModel</c> (the Configure* property setup
    /// and menu-action wiring) and the reactive rule/dependency wiring
    /// (InitializeRules/ManualRegisterDependencies + PasswordInputModeChanged_Action /
    /// CertificatePasswordInputModeChanged_Action / ProxyPasswordInputModeChanged_Action /
    /// FtpEncryptionModeChanged_Action / ProxyModeChanged_Action). None of this had a test before.
    /// <para>
    /// "Initial state" and "restored values" drive the SDK's own <c>InitializeAsync</c> pipeline end
    /// to end (InitializeModel, then InitializeRules/ManualRegisterDependencies, then ExecuteAll of
    /// the runOnInit rules registered by the 2-arg <c>Rule()</c> overload) rather than only calling
    /// the *_Changed_Action methods directly -- this is what actually proves the canvas ends up in
    /// the right state, instead of assuming the SDK wiring works. Setting a DesignProperty's
    /// <c>.Value</c> before calling <c>InitializeAsync()</c> simulates a persisted value a saved
    /// workflow restores before the canvas renders it; the SDK does not overwrite an already-set
    /// value when no matching persisted entry is passed in, so it survives into the render pass, and
    /// ExecuteAll's runOnInit rules react to whatever value is present at that point -- confirmed
    /// directly against the real pipeline, not simulated.
    /// </para>
    /// <para>
    /// "Mode transitions" call the *_Changed_Action methods directly, mirroring the pattern used for
    /// the Cryptography ViewModels' equivalent handlers (e.g. <c>FormatChanged_Action</c>): this is
    /// what the SDK's dependency-triggered dispatch does for a single already-initialized property
    /// change, without re-running the whole init pipeline.
    /// </para>
    /// </summary>
    public class WithFtpSessionViewModelTests
    {
        private static WithFtpSessionViewModel NewViewModel() =>
            new WithFtpSessionViewModel(Mock.Of<IDesignServices>());

        // --- Initial state: a brand-new activity, every switch at its schema default ---

        [Fact]
        public async Task InitialState_DefaultValues_HidesConditionalFields()
        {
            var vm = NewViewModel();

            await vm.InitializeAsync();

            // InitializeModel ran: the scope body is never a property row, and the primary
            // connection field carries its configured metadata.
            Assert.False(vm.Body.IsVisible);
            Assert.True(vm.Host.IsRequired);
            Assert.True(vm.Host.IsPrincipal);

            Assert.True(vm.Password.IsVisible);
            Assert.False(vm.SecurePassword.IsVisible);
            Assert.True(vm.ClientCertificatePassword.IsVisible);
            Assert.False(vm.ClientCertificateSecurePassword.IsVisible);
            Assert.False(vm.SslProtocols.IsVisible);      // FtpsMode == None
            Assert.False(vm.ProxyServer.IsVisible);        // ProxyType == None
            Assert.False(vm.ProxyPort.IsVisible);
            Assert.False(vm.ProxyUser.IsVisible);
            Assert.False(vm.ProxyPassword.IsVisible);
            Assert.False(vm.ProxySecurePassword.IsVisible);

            // The input-mode switches back menu actions only; they are never rendered themselves.
            Assert.False(vm.PasswordInputModeSwitch.IsVisible);
            Assert.False(vm.CertificatePasswordInputModeSwitch.IsVisible);
            Assert.False(vm.ProxyPasswordInputModeSwitch.IsVisible);
        }

        // --- Restored values: reopening a workflow where the switches were already set ---
        //
        // Setting .Value before InitializeAsync simulates the persisted state a saved workflow
        // restores into the DesignProperty before the canvas renders it. If the rules didn't run
        // against the restored value (or ran before InitializeModel's defaults were applied), these
        // fields would render with the wrong visibility on every reopen.

        [Fact]
        public async Task RestoredValues_SecurePasswordMode_ShowsSecurePasswordNotPassword()
        {
            var vm = NewViewModel();
            vm.PasswordInputModeSwitch.Value = PasswordInputMode.SecurePassword;

            await vm.InitializeAsync();

            Assert.False(vm.Password.IsVisible);
            Assert.True(vm.SecurePassword.IsVisible);
        }

        [Fact]
        public async Task RestoredValues_CertificateSecurePasswordMode_ShowsCertificateSecurePassword()
        {
            var vm = NewViewModel();
            vm.CertificatePasswordInputModeSwitch.Value = PasswordInputMode.SecurePassword;

            await vm.InitializeAsync();

            Assert.False(vm.ClientCertificatePassword.IsVisible);
            Assert.True(vm.ClientCertificateSecurePassword.IsVisible);
        }

        [Fact]
        public async Task RestoredValues_FtpsModeExplicit_ShowsSslProtocols()
        {
            var vm = NewViewModel();
            vm.FtpsMode.Value = FTP.FtpsMode.Explicit;

            await vm.InitializeAsync();

            Assert.True(vm.SslProtocols.IsVisible);
        }

        [Fact]
        public async Task RestoredValues_ProxyTypeHttp_ShowsProxyFields()
        {
            var vm = NewViewModel();
            vm.ProxyType.Value = FtpProxyType.Http;

            await vm.InitializeAsync();

            Assert.True(vm.ProxyServer.IsVisible);
            Assert.True(vm.ProxyPort.IsVisible);
            Assert.True(vm.ProxyUser.IsVisible);
            Assert.True(vm.ProxyPassword.IsVisible);       // default ProxyPasswordInputModeSwitch == Password
            Assert.False(vm.ProxySecurePassword.IsVisible);
        }

        [Fact]
        public async Task RestoredValues_ProxyTypeHttpWithSecurePassword_ShowsProxySecurePasswordNotProxyPassword()
        {
            var vm = NewViewModel();
            vm.ProxyType.Value = FtpProxyType.Http;
            vm.ProxyPasswordInputModeSwitch.Value = PasswordInputMode.SecurePassword;

            await vm.InitializeAsync();

            Assert.False(vm.ProxyPassword.IsVisible);
            Assert.True(vm.ProxySecurePassword.IsVisible);
        }

        // --- Mode transitions: the user flips a switch after the canvas is already open ---

        [Fact]
        public void ModeTransition_PasswordInputModeSwitch_TogglesPasswordFields()
        {
            var vm = NewViewModel();

            vm.PasswordInputModeSwitch.Value = PasswordInputMode.SecurePassword;
            vm.PasswordInputModeChanged_Action();
            Assert.False(vm.Password.IsVisible);
            Assert.True(vm.SecurePassword.IsVisible);

            vm.PasswordInputModeSwitch.Value = PasswordInputMode.Password;
            vm.PasswordInputModeChanged_Action();
            Assert.True(vm.Password.IsVisible);
            Assert.False(vm.SecurePassword.IsVisible);
        }

        [Fact]
        public void ModeTransition_CertificatePasswordInputModeSwitch_TogglesCertificatePasswordFields()
        {
            var vm = NewViewModel();

            vm.CertificatePasswordInputModeSwitch.Value = PasswordInputMode.SecurePassword;
            vm.CertificatePasswordInputModeChanged_Action();
            Assert.False(vm.ClientCertificatePassword.IsVisible);
            Assert.True(vm.ClientCertificateSecurePassword.IsVisible);

            vm.CertificatePasswordInputModeSwitch.Value = PasswordInputMode.Password;
            vm.CertificatePasswordInputModeChanged_Action();
            Assert.True(vm.ClientCertificatePassword.IsVisible);
            Assert.False(vm.ClientCertificateSecurePassword.IsVisible);
        }

        [Fact]
        public void ModeTransition_FtpsModeToNone_HidesSslProtocols()
        {
            var vm = NewViewModel();
            vm.FtpsMode.Value = FTP.FtpsMode.Explicit;
            vm.FtpEncryptionModeChanged_Action();
            Assert.True(vm.SslProtocols.IsVisible);

            vm.FtpsMode.Value = FTP.FtpsMode.None;
            vm.FtpEncryptionModeChanged_Action();
            Assert.False(vm.SslProtocols.IsVisible);
        }

        [Fact]
        public void ModeTransition_ProxyTypeOnThenOff_TogglesAllProxyFields()
        {
            var vm = NewViewModel();

            vm.ProxyType.Value = FtpProxyType.Http;
            vm.ProxyModeChanged_Action();
            Assert.True(vm.ProxyServer.IsVisible);
            Assert.True(vm.ProxyPort.IsVisible);
            Assert.True(vm.ProxyUser.IsVisible);
            Assert.True(vm.ProxyPassword.IsVisible);

            vm.ProxyType.Value = FtpProxyType.None;
            vm.ProxyModeChanged_Action();
            Assert.False(vm.ProxyServer.IsVisible);
            Assert.False(vm.ProxyPort.IsVisible);
            Assert.False(vm.ProxyUser.IsVisible);
            Assert.False(vm.ProxyPassword.IsVisible);
            Assert.False(vm.ProxySecurePassword.IsVisible);
        }

        [Fact]
        public void ModeTransition_ProxyPasswordInputModeSwitch_TogglesProxyPasswordFieldsWhileProxyOn()
        {
            var vm = NewViewModel();
            vm.ProxyType.Value = FtpProxyType.Http;
            vm.ProxyModeChanged_Action();

            vm.ProxyPasswordInputModeSwitch.Value = PasswordInputMode.SecurePassword;
            vm.ProxyPasswordInputModeChanged_Action();

            Assert.False(vm.ProxyPassword.IsVisible);
            Assert.True(vm.ProxySecurePassword.IsVisible);
        }

        // Pins a real asymmetry in the current implementation: ProxyPasswordInputModeChanged_Action
        // does not consult ProxyType, unlike ProxyModeChanged_Action which recomputes all five proxy
        // fields together. In the live canvas this switch's menu is only reachable from the
        // ProxyPassword/ProxySecurePassword fields themselves, which are hidden while proxy is off,
        // so the action should not fire in that state in practice -- but if it ever did, it would
        // incorrectly reveal ProxyPassword even though the rest of the proxy section stays hidden.
        [Fact]
        public void ModeTransition_ProxyPasswordInputModeSwitch_IgnoresProxyOffState_KnownAsymmetry()
        {
            var vm = NewViewModel();
            // ProxyType left at its default (None) -- proxy section is off.

            vm.ProxyPasswordInputModeSwitch.Value = PasswordInputMode.Password;
            vm.ProxyPasswordInputModeChanged_Action();

            Assert.True(vm.ProxyPassword.IsVisible);
            Assert.False(vm.ProxySecurePassword.IsVisible);
        }
    }
}

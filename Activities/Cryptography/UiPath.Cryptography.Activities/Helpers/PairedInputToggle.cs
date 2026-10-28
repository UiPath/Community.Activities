using System;
using System.Activities;
using System.Activities.DesignViewModels;
using System.Activities.ViewModels;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace UiPath.Cryptography.Activities.Helpers
{
    /// <summary>
    /// Shared design-time toggle for any pair of mutually-exclusive
    /// <see cref="DesignInArgument{T}"/> properties (string ↔ SecureString,
    /// path ↔ IResource, etc.).
    ///
    /// Encapsulates the switch handlers and the persistence of the inactive side.
    /// Hosts supply <see cref="SwitchGuard"/> for context-sensitive blocking
    /// (e.g. don't switch when Algorithm != PGP) and <see cref="AfterSwitch"/>
    /// for visibility recomputation.
    /// </summary>
    [ExcludeFromCodeCoverage]
    internal sealed class PairedInputToggle<TPrimary, TSecondary>
        where TPrimary : class
        where TSecondary : class
    {
        private readonly DesignInArgument<TPrimary> _primary;
        private readonly DesignInArgument<TSecondary> _secondary;
        private readonly string _switchToPrimaryLabel;
        private readonly string _switchToSecondaryLabel;
        private InArgument<TPrimary> _persistedPrimary;
        private InArgument<TSecondary> _persistedSecondary;

        public bool UseSecondary { get; private set; }
        public Func<bool> SwitchGuard { get; set; } = () => false;
        public Action AfterSwitch { get; set; }

        public PairedInputToggle(
            DesignInArgument<TPrimary> primary,
            DesignInArgument<TSecondary> secondary,
            string switchToPrimaryLabel,
            string switchToSecondaryLabel)
        {
            _primary = primary;
            _secondary = secondary;
            _switchToPrimaryLabel = switchToPrimaryLabel;
            _switchToSecondaryLabel = switchToSecondaryLabel;
        }

        [SuppressMessage("Sonar", "S1144:Unused private types or members should be removed",
            Justification = "Handlers are referenced indirectly via MenuAction.")]
        public void ConfigureMenuActions()
        {
            _secondary.AddMenuAction(new MenuAction
            {
                DisplayName = _switchToPrimaryLabel,
                IsMain = true,
                Handler = SwitchToPrimary,
            });
            _primary.AddMenuAction(new MenuAction
            {
                DisplayName = _switchToSecondaryLabel,
                IsMain = true,
                Handler = SwitchToSecondary,
            });

            UseSecondary = _secondary.HasValue && !_primary.HasValue;
        }

        private Task SwitchToPrimary(MenuAction _)
        {
            if (SwitchGuard()) return Task.CompletedTask;
            if (_secondary.Value != null) _persistedSecondary = _secondary.Value;
            _secondary.Value = null;
            _primary.Value = _persistedPrimary;
            UseSecondary = false;
            AfterSwitch?.Invoke();
            return Task.CompletedTask;
        }

        private Task SwitchToSecondary(MenuAction _)
        {
            if (SwitchGuard()) return Task.CompletedTask;
            if (_primary.Value != null) _persistedPrimary = _primary.Value;
            _primary.Value = null;
            _secondary.Value = _persistedSecondary;
            UseSecondary = true;
            AfterSwitch?.Invoke();
            return Task.CompletedTask;
        }
    }
}

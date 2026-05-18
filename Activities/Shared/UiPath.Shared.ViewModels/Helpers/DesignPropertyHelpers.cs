using System.Activities.DesignViewModels;
using System.Threading.Tasks;

namespace UiPath.Shared.ViewModels.Helpers
{
    /// <summary>
    /// Manages toggling visibility between two mutually exclusive <see cref="DesignProperty"/> instances,
    /// preserving each property's value so it can be restored when toggled back.
    /// </summary>
    internal sealed class DesignPropertyToggle<T1, T2>
        where T1 : DesignProperty
        where T2 : DesignProperty
    {
        private readonly T1 _first;
        private readonly T2 _second;
        private readonly bool _setIsRequiredWhenVisible;
        private object _savedFirst;
        private object _savedSecond;

        /// <param name="setIsRequiredWhenVisible">
        /// When <see langword="true"/> (default), <see cref="DesignProperty.IsRequired"/> is set to
        /// <see langword="true"/> on the visible property and <see langword="false"/> on the hidden one.
        /// When <see langword="false"/>, <see cref="DesignProperty.IsRequired"/> is never modified.
        /// </param>
        public DesignPropertyToggle(T1 first, T2 second, bool setIsRequiredWhenVisible = true)
        {
            _first = first;
            _second = second;
            _setIsRequiredWhenVisible = setIsRequiredWhenVisible;
        }

        /// <summary>
        /// Sets the initial visibility state without touching values (they are already loaded from the model).
        /// </summary>
        public void Initialize(bool showFirst)
        {
            if (showFirst)
            {
                _first.IsVisible = true;
                _second.IsVisible = false;
                if (_setIsRequiredWhenVisible)
                {
                    _first.IsRequired = true;
                    _second.IsRequired = false;
                }
            }
            else
            {
                _second.IsVisible = true;
                _first.IsVisible = false;
                if (_setIsRequiredWhenVisible)
                {
                    _second.IsRequired = true;
                    _first.IsRequired = false;
                }
            }
        }

        /// <summary>
        /// Shows the first property and hides the second, saving and restoring values.
        /// </summary>
        public Task ShowFirst()
        {
            _savedSecond = _second.Value;
            _second.Value = null;
            _second.IsVisible = false;
            if (_setIsRequiredWhenVisible)
                _second.IsRequired = false;

            _first.IsVisible = true;
            if (_setIsRequiredWhenVisible)
                _first.IsRequired = true;
            _first.Value = _savedFirst;

            return Task.CompletedTask;
        }

        /// <summary>
        /// Shows the second property and hides the first, saving and restoring values.
        /// </summary>
        public Task ShowSecond()
        {
            _savedFirst = _first.Value;
            _first.Value = null;
            _first.IsVisible = false;
            if (_setIsRequiredWhenVisible)
                _first.IsRequired = false;

            _second.IsVisible = true;
            if (_setIsRequiredWhenVisible)
                _second.IsRequired = true;
            _second.Value = _savedSecond;

            return Task.CompletedTask;
        }
    }
}

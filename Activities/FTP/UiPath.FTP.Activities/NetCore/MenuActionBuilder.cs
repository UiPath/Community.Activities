using System;
using System.Activities.DesignViewModels;
using System.Activities.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

//NOTE: This class was taken from Activities repo
namespace UiPath.FTP.Activities.NetCore
{
    /// <summary>
    /// Builds and manages related <see cref="MenuAction">menu actions</see> for <see cref="DesignProperty">properties</see> in a <see cref="DesignPropertiesViewModel"/>.<br/>
    /// If the type parameter is an <see langword="enum"/> and no other function is provided, the <see cref="LocalizedEnum.GetLocalizedValue(Type, object)"/> method will be used by default to retrieve display names.
    /// </summary>
    /// <typeparam name="T">Type of the values that are associated with menu actions.</typeparam>
    internal class MenuActionsBuilder<T>
    {
        private readonly DesignProperty<T> _valueProperty;
        private readonly List<MenuActionInfo<T>> _properties = new List<MenuActionInfo<T>>();

        private Func<T, string> _getDisplayName;

        /// <summary>
        /// Creates a <see cref="MenuActionsBuilder{T}"/> from a given value property.
        /// </summary>
        /// <param name="valueProperty">A property that will have its value set when menu actions are triggered.</param>
        public MenuActionsBuilder(DesignProperty<T> valueProperty)
        {
            _valueProperty = valueProperty ?? throw new ArgumentNullException(nameof(valueProperty));

            if (typeof(Enum).IsAssignableFrom(typeof(T)))
                _getDisplayName = ism => LocalizedEnum.GetLocalizedValue(typeof(T), ism).Name;
        }

        /// <summary>
        /// Creates a <see cref="MenuActionsBuilder{T}"/> from a given value property.
        /// </summary>
        /// <param name="valueProperty">A property that will have its value set when menu actions are triggered.</param>
        public static MenuActionsBuilder<T> WithValueProperty(DesignProperty<T> valueProperty) => new MenuActionsBuilder<T>(valueProperty);

        /// <summary>
        /// Stores an optional function that will be called to retrieve the display name of a given <typeparamref name="T"/> value.<br/>
        /// If <typeparamref name="T"/> is an <see langword="enum"/> and no other function is provided, the <see cref="LocalizedEnum.GetLocalizedValue(Type, object)"/> method will be used by default to retrieve display names.
        /// </summary>
        public MenuActionsBuilder<T> WithDisplayNameGetter(Func<T, string> getDisplayName)
        {
            _getDisplayName = getDisplayName ?? throw new ArgumentNullException(nameof(getDisplayName));

            return this;
        }

        /// <summary>
        /// Stores the given property and its related <typeparamref name="T"/> value.
        /// </summary>
        /// <param name="property">A property that will later have <see cref="MenuAction">menu actions</see> assigned.</param>
        /// <param name="value">The related <typeparamref name="T"/> value.</param>
        /// <param name="displayName">Optional display name. If not provided, a display name function has to be specified via <see cref="WithDisplayNameGetter(Func{T, string})"/></param>
        public MenuActionsBuilder<T> AddMenuProperty(DesignProperty property, T value, string displayName = null)
        {
            if (property == null)
                throw new ArgumentNullException(nameof(property));

            if (displayName == null && _getDisplayName != null)
                displayName = _getDisplayName(value);

            if (string.IsNullOrWhiteSpace(displayName))
                throw new ArgumentNullException(nameof(displayName));

            _properties.Add(new MenuActionInfo<T>(property, value, displayName));

            return this;
        }

        /// <summary>
        /// For each of the stored properties, adds <see cref="MenuAction">menu actions</see> that represent all the other stored properties.
        /// </summary>
        public void BuildAndInsertMenuActions()
        {
            foreach (var targetPropertyInfo in _properties)
            {
                var menuAction = new MenuAction
                {
                    DisplayName = targetPropertyInfo.DisplayName,
                    Handler = _ =>
                    {
                        _valueProperty.Value = targetPropertyInfo.Value;

                        return Task.CompletedTask;
                    }
                };
                foreach (var propertyInfo in _properties.Where(pi => pi != targetPropertyInfo))
                {
                   

                    propertyInfo.Property.AddMenuAction(menuAction);
                }
            }
        }

        /// <summary>
        /// Wrapper class for the properties needed to build a <see cref="MenuAction"/>.
        /// </summary>
        private class MenuActionInfo<T1>
        {
            public MenuActionInfo(DesignProperty property, T1 value, string displayName)
            {
                if (string.IsNullOrWhiteSpace(displayName))
                    throw new ArgumentNullException(nameof(displayName));

                Property = property ?? throw new ArgumentNullException(nameof(property));
                Value = value ?? throw new ArgumentNullException(nameof(value));
                DisplayName = displayName;
            }
            public DesignProperty Property { get; set; }
            public T1 Value { get; set; }
            public string DisplayName { get; set; }
        }
    }
}

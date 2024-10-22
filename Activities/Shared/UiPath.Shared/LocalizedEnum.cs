using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;

namespace UiPath.Shared
{
    internal class LocalizedEnum
    {
        public string Name { get; private set; }
        public Enum Value { get; private set; }

        protected internal LocalizedEnum(string name, Enum value)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException(nameof(name));
            }
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            Name = name;
            Value = value;
        }

        /// <summary>
        /// Method that returns a localized enum with the description as a name, if the description exists.
        /// </summary>
        /// <param name="enumType"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static LocalizedEnum GetLocalizedValue(Type enumType, object value)
        {
            var name = enumType.GetEnumName(value);
            var field = enumType.GetField(name);
            DescriptionAttribute descriptionAttribute = field?.GetCustomAttribute<DescriptionAttribute>();

            return new LocalizedEnum(descriptionAttribute?.Description ?? name, value as Enum);
        }
    }

    internal class LocalizedEnum<T> : LocalizedEnum
    {
        protected LocalizedEnum(string name, Enum value) : base(name, value)
        {
        }

        public static List<LocalizedEnum> GetLocalizedValues()
        {
            List<LocalizedEnum> localizedValues = new List<LocalizedEnum>();

            Type enumType = typeof(T);
            Array enumValues = Enum.GetValues(enumType);

            foreach (Enum value in enumValues)
            {
                string name = enumType.GetEnumName(value);
                FieldInfo field = enumType.GetField(name);
                DescriptionAttribute descriptionAttribute = field?.GetCustomAttribute<DescriptionAttribute>();

                localizedValues.Add(new LocalizedEnum(descriptionAttribute?.Description ?? name, value));
            }

            return localizedValues;
        }
    }
}
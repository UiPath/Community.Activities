﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;

namespace UiPath.Shared
{
    public class LocalizedEnum
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
    }

    public class LocalizedEnum<T> : LocalizedEnum
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
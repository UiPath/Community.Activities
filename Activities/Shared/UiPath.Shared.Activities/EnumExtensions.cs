using System;
using System.Collections.Generic;
using System.Linq;

namespace UiPath.Shared.Activities
{
    internal static class EnumExtensions<T> where T : struct, Enum
    {
        /// <summary>
        /// Splits a flag value into its composing bits
        /// </summary>
        /// <param name="value"></param>
        /// <returns>List of bits</returns>
        public static List<T> Decompose(T value)
        {
            if(value.GetType().GetCustomAttributes(typeof(FlagsAttribute), false).Length == 0)
            {
                return new List<T> { value };
            }

            var enumValue = value as Enum;

            // try first to match a single enum value that covers multiple flags
            foreach (var flag in Enum.GetValues(typeof(T)).Cast<Enum>())
            {
                if (Equals(flag, enumValue))
                {
                    return new List<T>() { value };
                }
            }

            // if no single value matches, decompose it into the flags it contains
            var list = new List<Enum>();
            foreach (var flag in Enum.GetValues(typeof(T)).Cast<Enum>())
            {
                if (enumValue.HasFlag(flag))
                {
                    list.Add(flag);
                }
            }
            return list.Cast<T>().ToList();
        }
    }
}

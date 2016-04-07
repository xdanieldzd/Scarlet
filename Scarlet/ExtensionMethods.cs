using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Scarlet
{
    internal static class ExtensionMethods
    {
        public static T Clamp<T>(this T val, T min, T max) where T : IComparable<T>
        {
            if (val.CompareTo(min) < 0) return min;
            else if (val.CompareTo(max) > 0) return max;
            else return val;
        }

        /* http://stackoverflow.com/a/23177585 */
        public static bool IsValid<TEnum>(this TEnum enumValue) where TEnum : struct
        {
            var firstChar = enumValue.ToString()[0];
            return (firstChar < '0' || firstChar > '9') && firstChar != '-';
        }
    }
}

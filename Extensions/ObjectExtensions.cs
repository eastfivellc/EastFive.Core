using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlackBarLabs.Core.Extensions // Make user force extensions because this affects _every_ object
{
    public static class ObjectExtensions
    {
        public static T OrIfDefault<T>(this T value, T alternative)
            where T : IComparable
        {
            if (value.IsDefault())
                return alternative;
            return value;
        }

        public static T OrIfDefaultOrNull<T>(this T value, T alternative)
            where T : class
        {
            if (value.IsDefaultOrNull())
                return alternative;
            return value;
        }

        public static bool IsDefault<T>(this T value)
            where T : IComparable
        {
            return EqualityComparer<T>.Default.Equals(value, default(T));
        }

        public static bool IsDefaultOrNull<T>(this T value)
           where T : class
        {
            return EqualityComparer<T>.Default.Equals(value, default(T));
        }

        public static bool IsDefaultOrEmpty(this Guid value)
        {
            return value.IsDefault() || value == Guid.Empty;
        }

        public static TResult HasValue<TResult>(this Guid value, Func<Guid, TResult> hasValue, Func<TResult> nullOrEmptyValue)
        {
            if (value.IsDefaultOrEmpty())
                return nullOrEmptyValue();
            return hasValue(value);
        }

        public static TResult HasValue<TResult>(this DateTime value, Func<DateTime, TResult> hasValue, Func<TResult> nullOrEmptyValue)
        {
            if (value.IsDefault())
                return nullOrEmptyValue();
            return hasValue(value);
        }

        public static bool IsDefault(this Uri value)
        {
            return value.IsDefaultOrNull();
        }
    }
}

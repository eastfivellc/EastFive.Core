using BlackBarLabs.Collections.Generic;
using BlackBarLabs.Linq;
using EastFive.Collections.Generic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BlackBarLabs.Extensions;
using EastFive.Linq;

namespace BlackBarLabs.Extensions // Make user force extensions because this affects _every_ object
{
    public static class ObjectExtensions
    {
        public static IEnumerable<T> AsEnumerable<T>(this T onlyItem)
        {
            yield return onlyItem;
        }
        
        public static Task<T> ToTask<T>(this T value)
        {
            return Task.FromResult(value);
        }

        public static KeyValuePair<TKey, TValue> PairWithValue<TKey, TValue>(this TKey key, TValue value)
        {
            return new KeyValuePair<TKey, TValue>(key, value);
        }
    }
}

namespace EastFive.Extensions
{
    public static class ObjectExtensions
    {
        public static KeyValuePair<TKey, TValue> PairWithKey<TKey, TValue>(this TValue value, TKey key)
        {
            return new KeyValuePair<TKey, TValue>(key, value);
        }

        #region AsArray

        public static T[] AsArray<T>(this T onlyItem)
        {
            return new T[] { onlyItem };
        }

        public static T[] AsArray<T>(this T firstItem, T secondItem)
        {
            return new T[] { firstItem, secondItem, };
        }

        public static T[] AsArray<T>(this T firstItem, T secondItem, T thirdItem)
        {
            return new T[] { firstItem, secondItem, thirdItem };
        }

        #endregion

        public static IEnumerable<KeyValuePair<TKey, TValue>> PairWithValues<TKey, TValue>(this IEnumerable<TKey> keys, IEnumerable<TValue> values)
        {
            var valuesIterator = values.GetEnumerator();
            return keys.Select(
                (key, index) =>
                {
                    if (!valuesIterator.MoveNext())
                        return default(KeyValuePair<TKey, TValue>?);
                    return key.PairWithValue(valuesIterator.Current);
                })
                .SelectWhereHasValue();
        }

        public static RecursiveTuple<TKey> RecurseWithValue<TKey>(this TKey key, RecursiveTuple<TKey> value)
        {
            return new RecursiveTuple<TKey>()
            {
                item1 = key,
                next = () => value,
            };
        }
        public static IDictionary<TKey, TValue> AsDictionary<TKey, TValue>(this KeyValuePair<TKey, TValue> onlyItem)
        {
            return onlyItem.AsEnumerable().ToDictionary();
        }

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
        {
            return EqualityComparer<T>.Default.Equals(value, default(T));
        }

        public static bool IsDefaultOrNull<T>(this T value)
           where T : class
        {
            return EqualityComparer<T>.Default.Equals(value, default(T));
        }

        public static TResult HasValue<T, TResult>(this T value, Func<T, TResult> hasValue, Func<TResult> nullOrEmptyValue)
           where T : class
        {
            if (value.IsDefaultOrNull())
                return nullOrEmptyValue();
            return hasValue(value);
        }

        public static bool IsDefaultOrEmpty(this Guid value)
        {
            return value.IsDefault() || value == Guid.Empty;
        }

        public static TResult HasValue<T, TResult>(this Nullable<T> value, Func<T, TResult> hasValue, Func<TResult> nullOrEmptyValue)
            where T : struct
        {
            if (!value.HasValue)
                return nullOrEmptyValue();
            return hasValue(value.Value);
        }

        public static bool Equals<T>(this Nullable<T> value1, Nullable<T> value2)
           where T : struct
        {
            if (value1.HasValue)
            {
                if (value2.HasValue)
                    return ValueType.Equals(value1.Value, value2.Value);
                return false;
            }
            return !value2.HasValue;
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

        public static T? ToOptional<T>(this T value)
            where T : struct
        {
            return (T?)value;
        }

        public static bool IsDefault(this Uri value)
        {
            return value.IsDefaultOrNull();
        }

        public static T IfThen<T>(this T value, bool ifCondition,
            Func<T, T> thenOperation)
        {
            if (ifCondition)
                return thenOperation(value);
            return value;
        }
    }
}

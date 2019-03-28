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
        public static IEnumerable<T> AsEnumerable<T>(this T firstItem,
            Func<T, IEnumerable<T>, Func<T, IEnumerable<T>>, IEnumerable<T>> nextItem
                = default(Func<T, IEnumerable<T>, Func<T, IEnumerable<T>>, IEnumerable<T>>))
        {
            yield return firstItem;
        }

        public static KeyValuePair<TKey, TValue> PairWithKey<TKey, TValue>(this TValue value, TKey key)
        {
            return new KeyValuePair<TKey, TValue>(key, value);
        }

        public static Task<T> AsTask<T>(this T value)
        {
            return Task.FromResult(value);
        }

        public static T? AsOptional<T>(this T value)
            where T : struct
        {
            return (T?)value;
        }

        public static async Task<T?> AsOptional<T>(this Task<T> value)
            where T : struct
        {
            return await value;
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

        public static IEnumerable<KeyValuePair<TKey, TValue>> PairWithValues<TKey, TValue>(this IEnumerable<TKey> keys, Func<TKey, TValue> calcValue)
        {
            foreach (var key in keys.NullToEmpty())
                yield return key.PairWithValue(calcValue(key));
        }

        public static RecursiveTuple<TKey> RecurseWithValue<TKey>(this TKey key, RecursiveTuple<TKey> value)
        {
            return new RecursiveTuple<TKey>()
            {
                item1 = key,
                next = () => value,
            };
        }

        public static TResult RecurseWithValue<TItem, TIndex, TResult>(this TItem key, TIndex index,
            Func<TItem, TIndex, Func<TItem, TIndex, TResult>, TResult> value)
        {
            return value(key, index,
                (itemNext, indexNext) => RecurseWithValue(itemNext, indexNext, value));
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

        public static bool IsDefaultNullOrEmpty<T>(this T[] value)
        {
            if (value.IsDefaultOrNull())
                return true;
            return (!value.Any());
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

        public static TResult TryAsDecimal<TResult>(this object value,
            Func<decimal, TResult> onCasted,
            Func<TResult> onCouldNotCast)
        {
            if (value is decimal)
            {
                var decimalValue = (decimal)value;
                return onCasted(decimalValue);
            }
            if (value is int)
            {
                var intValue = (int)value;
                return onCasted(intValue);
            }
            if (value is long)
            {
                var longValue = (long)value;
                return onCasted(longValue);
            }
            if (value is float)
            {
                var floatValue = (float)value;
                return onCasted((decimal)floatValue);
            }
            if (value is double)
            {
                var doubleValue = (double)value;
                return onCasted((decimal)doubleValue);
            }
            return onCouldNotCast();
        }
    }
}

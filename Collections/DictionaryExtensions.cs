using BlackBarLabs.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BlackBarLabs.Collections.Generic
{
    public static class DictionaryExtensions
    {
        public static void AddOrReplace<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue value)
        {
            if (dictionary.ContainsKey(key))
            {
                dictionary[key] = value;
                return;
            }
            dictionary.Add(key, value);
        }

        public static Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> kvpItems)
        {
            return kvpItems.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        public static IDictionary<TKey, TValue> Append<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue value)
        {
            dictionary.Add(key, value);
            return dictionary;
        }

        public static Dictionary<TKey, TValue[]> ToDictionaryDistinct<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> kvpItems,
            Func<TKey, int> hash = default(Func<TKey, int>))
            where TKey : IComparable<TKey>
        {
            Func<KeyValuePair<TKey, TValue>, KeyValuePair<TKey, TValue>, int> comparison =
                (kvp1, kvp2) => kvp1.Key.CompareTo(kvp2.Key);
            return kvpItems
                .Distinct(comparison.ToEqualityComparer())
                .SelectKeys()
                .Select(key => key.PairWithValue(
                    kvpItems
                        .Where(kvp => key.CompareTo(kvp.Key) == 0)
                        .Select(kvp => kvp.Value)
                        .ToArray()))
                .ToDictionary();
        }
        
        public static Dictionary<TKey, TValue[]> ToDictionaryCollapsed<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> kvpItems,
            Func<TKey, TKey, bool> areEqual,
            Func<TKey, int> hash = default(Func<TKey, int>))
        {
            Func<KeyValuePair<TKey, TValue>, KeyValuePair<TKey, TValue>, int> fullComparison =
                (kvp1, kvp2) => areEqual(kvp1.Key, kvp2.Key)? 0 : -1;
            return kvpItems
                .Distinct(fullComparison.ToEqualityComparer())
                .SelectKeys()
                .Select(key => key.PairWithValue(
                    kvpItems
                        .Where(kvp => areEqual(key, kvp.Key))
                        .Select(kvp => kvp.Value)
                        .ToArray()))
                .ToDictionary();
        }

        public static IEnumerable<TValue> SelectValues<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> dictionary)
        {
            return dictionary.Select(kvp => kvp.Value);
        }

        public static IEnumerable<TKey> SelectKeys<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> dictionary)
        {
            return dictionary.Select(kvp => kvp.Key);
        }
    }
}

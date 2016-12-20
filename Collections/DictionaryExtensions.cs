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

        public static Dictionary<TKey, TValue> ToDictionaryDistinct<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> kvpItems)
            where TKey : IComparable<TKey>
        {
            Func<KeyValuePair<TKey, TValue>, KeyValuePair<TKey, TValue>, int> comparison =
                (kvp1, kvp2) => kvp1.Key.CompareTo(kvp2.Key);
            return kvpItems
                .Distinct(comparison.ToEqualityComparer())
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
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

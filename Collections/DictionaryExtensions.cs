using BlackBarLabs.Collections.Generic;
using BlackBarLabs.Extensions;
using BlackBarLabs.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EastFive.Collections.Generic
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

        public static TResult ToDictionary<TKey, TValue, TKeyDictionary, TValueDictionary, TResult>(this IEnumerable<KeyValuePair<TKey, TValue>> kvpItems,
            Func<KeyValuePair<TKey, TValue>, TKeyDictionary> selectKey,
            Func<KeyValuePair<TKey, TValue>, TValueDictionary> selectValue,
            Func<Dictionary<TKeyDictionary, TValueDictionary>, KeyValuePair<TKey, TValue>[], TResult> dictionaryAndDuplicates)
        {
            var hashSet = new HashSet<TKey>();
            var kvpItemsArray = kvpItems.ToArray();
            var duplicates = new KeyValuePair<TKey, TValue>[] { };
            var dictionary = kvpItems
                .Select(
                    kvp =>
                    {
                        if (hashSet.Contains(kvp.Key))
                        {
                            duplicates = duplicates.Append(kvp).ToArray();
                            return default(KeyValuePair<TKeyDictionary, TValueDictionary>?);
                        }
                        return selectKey(kvp).PairWithValue(selectValue(kvp));
                    })
                .SelectWhereHasValue()
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            return dictionaryAndDuplicates(dictionary, duplicates);
        }

        public static TResult ToDictionary<TKey, TValue, TResult>(this IEnumerable<KeyValuePair<TKey, TValue>> kvpItems,
            Func<Dictionary<TKey, TValue>, KeyValuePair<TKey, TValue>[], TResult> dictionaryAndDuplicates)
        {
            return kvpItems.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value,
                dictionaryAndDuplicates);
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

        public static TResult WhereKey<TKey, TValue, TResult>(this IDictionary<TKey, TValue> dictionary,
                Func<KeyValuePair<TKey, TValue>, bool> predicate,
            Func<KeyValuePair<TKey, TValue>, TResult> onKeyFound,
            Func<TResult> onKeyNotFound)
        {
            var matching = dictionary.Where(predicate).ToArray();
            if (matching.Length > 0)
                return onKeyFound(matching[0]);
            return onKeyNotFound();
        }
        
        public static HashSet<T> AsHashSet<T>(this IEnumerable<T> items)
        {
            return new HashSet<T>(items);
        }

        public static Func<T, bool> AsPredicate<T>(this HashSet<T> lookups)
        {
            return (v) => lookups.Contains(v);
        }

        public static Func<TPredicate, bool> AsPredicate<T, TPredicate>(this HashSet<T> lookups,
            Func<TPredicate, T> translation)
        {
            return (v) => lookups.Contains(translation(v));
        }

        public static IDictionary<string, string> AsDictionary(this System.Collections.Specialized.NameValueCollection nameValueCollection)
        {
            return nameValueCollection.AllKeys
                .Select(key => key.PairWithValue(nameValueCollection[key]))
                .ToDictionary();
        }
    }
}

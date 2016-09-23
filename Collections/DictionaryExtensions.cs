using System.Collections.Generic;
using System.Linq;

namespace BlackBarLabs.Core.Collections
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
    }
}

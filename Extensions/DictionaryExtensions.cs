using System;
using System.Collections.Generic;
using System.Linq;

namespace BlackBarLabs
{
    public static class DictionaryExtensions
    {
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

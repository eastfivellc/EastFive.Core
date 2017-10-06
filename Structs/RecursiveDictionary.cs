using BlackBarLabs.Extensions;
using BlackBarLabs.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlackBarLabs.Collections.Generic
{
    public class RecursiveDictionary<TKey> : Dictionary<TKey, RecursiveDictionary<TKey>>
    {
    }

    public static class RecursiveDictionaryExtensions
    {

        public static IEnumerable<T> Flatten<T>(this RecursiveDictionary<T> dictionary)
        {
            return dictionary
                .SelectMany(
                    kvp =>
                    {
                        if (kvp.Value.IsDefault())
                            return new T[] { kvp.Key };
                        return kvp.Value.Flatten().Append(kvp.Key);
                    });
        }

        public static IEnumerable<T> Bottom<T>(this RecursiveDictionary<T> dictionary)
        {
            return dictionary
                .SelectMany(n =>
                    n.Value.IsDefault()?
                    n.Key.ToEnumerable()
                    :
                    n.Value.Bottom());
        }

        public static RecursiveDictionary<TResult> SelectRecursive<TSource, TResult>(this RecursiveDictionary<TSource> dictionary,
            Func<TSource, TResult> selector)
        {
            return dictionary
                .Select(
                    kvp =>
                    {
                        return selector(kvp.Key)
                            .PairWithValue(
                                kvp.Value.IsDefault()?
                                    default(RecursiveDictionary<TResult>)
                                :
                                    kvp.Value.SelectRecursive(selector));
                    })
                .AsRecursive();
        }

        public static RecursiveDictionary<T> AsRecursive<T>(this IDictionary<T, RecursiveDictionary<T>> dictionary)
        {
            var recursive = new RecursiveDictionary<T>();
            foreach (var kvp in dictionary)
                recursive.Add(kvp.Key, kvp.Value);
            return recursive;
        }

        public static RecursiveDictionary<T> AsRecursive<T>(this IEnumerable<KeyValuePair<T, RecursiveDictionary<T>>> kvps)
        {
            var recursive = new RecursiveDictionary<T>();
            foreach (var kvp in kvps)
                recursive.Add(kvp.Key, kvp.Value);
            return recursive;
        }
    }

}

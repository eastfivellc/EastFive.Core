﻿using BlackBarLabs.Extensions;
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

    public class RecursiveDictionary<TKey, TValue> : Dictionary<TKey, RecursiveDictionary<TKey, TValue>>
    {
        public TValue Value { get; set; }
    }

    public static class RecursiveDictionaryExtensions
    {
        public static RecursiveDictionary<TKey, TValue> Add<TKey, TValue>(this RecursiveDictionary<TKey, TValue> dictionary, TKey key, TValue value)
        {
            var next = new RecursiveDictionary<TKey, TValue>();
            next.Value = value;
            dictionary.Add(key, next);
            return dictionary;
        }

        public static RecursiveDictionary<TKey, TValue> AddIfMissing<TKey, TValue>(this RecursiveDictionary<TKey, TValue> dictionary, TKey key, TValue value)
        {
            if (dictionary.ContainsKey(key))
                return dictionary;
            return dictionary.Add(key, value);
        }

        public static RecursiveDictionary<TKey, TValue> AddWithRecursion<TKey, TValue>(this RecursiveDictionary<TKey, TValue> dictionary,
            TKey key, TValue value,
            Action<RecursiveDictionary<TKey, TValue>, Action<TKey, TValue>> callback)
        {
            if (dictionary.ContainsKey(key))
                return dictionary;
            dictionary.Add(key, value);
            var x = dictionary[key];
            x.WithRecursion(callback);
            return dictionary;
        }

        private static void WithRecursion<TKey, TValue>(this RecursiveDictionary<TKey, TValue> dictionary,
            Action<RecursiveDictionary<TKey, TValue>, Action<TKey, TValue>> callback)
        {
            callback(dictionary,
                (key, value) => dictionary.AddWithRecursion(key, value, callback));
        }

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
                    n.Key.AsEnumerable()
                    :
                    n.Value.Bottom());
        }

        public static RecursiveDictionary<TResult> SelectRecursive<TSource, TResult>(this RecursiveDictionary<TSource> dictionary,
            Func<TSource, TResult> selector)
        {
            if (dictionary.IsDefault())
                return default(RecursiveDictionary<TResult>);
            return dictionary
                .Select(
                    kvp =>
                    {
                        return selector(kvp.Key)
                            .PairWithValue(
                                //kvp.Value.IsDefault()?
                                //    default(RecursiveDictionary<TResult>)
                                //:
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
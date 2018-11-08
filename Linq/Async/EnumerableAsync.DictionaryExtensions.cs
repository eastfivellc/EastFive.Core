using EastFive.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EastFive.Linq.Async
{
    public static partial class EnumerableAsync
    {

        private class DictionaryAsync<TElement, TKey, TValue> : IDictionaryAsync<TKey, TValue>
        {
            // TODO: Use singular enumeration

            private IEnumerableAsync<TElement> enumerable;
            private Dictionary<TKey, TValue> dictionary;
            private Func<TElement, TKey> keySelector;
            private Func<TElement, TValue> valueSelector;


            public DictionaryAsync(IEnumerableAsync<TElement> enumerable,
                Func<TElement, TKey> keySelector,
                Func<TElement, TValue> valueSelector)
            {
                this.enumerable = enumerable;
                this.dictionary = new Dictionary<TKey, TValue>();
                this.keySelector = keySelector;
                this.valueSelector = valueSelector;
            }

            public Task<TValue> this[TKey key]
            {
                get
                {
                    return TryGetValue(key,
                        (v) => v,
                        () =>
                        {
                            throw new KeyNotFoundException(); 
                        });
                }
            }

            public IEnumerableAsync<TKey> Keys => enumerable.Select(this.keySelector);

            public IEnumerableAsync<TValue> Values => enumerable.Select(this.valueSelector);

            public Task<bool> ContainsKeyAsync(TKey key)
            {
                return TryGetValue(key,
                    (v) => true,
                    () => false);
            }

            public IEnumeratorAsync<KeyValuePair<TKey, TValue>> GetEnumerator()
                => enumerable
                    .Select(
                        item => this.valueSelector(item).PairWithKey(this.keySelector(item)))
                    .GetEnumerator();

            public async Task<TResult> TryGetValue<TResult>(TKey key, 
                Func<TValue, TResult> onValue,
                Func<TResult> onNotFound)
            {
                return await enumerable.FirstAsyncMatchAsync(
                    async (element, next) =>
                    {
                        var elementKey = this.keySelector(element);
                        if (elementKey.Equals(key))
                            return onValue(this.valueSelector(element));
                        return await next();
                    },
                    () => onNotFound());
            }
        }

        public static IDictionaryAsync<TKey, TValue> ToDictionary<TElement, TKey, TValue>(this IEnumerableAsync<TElement> enumerable,
            Func<TElement, TKey> keySelector,
            Func<TElement, TValue> valueSelector)
        {
            return new DictionaryAsync<TElement, TKey, TValue>(enumerable, keySelector, valueSelector);
        }

        public static IDictionaryAsync<TKey, TValue> ToDictionary<TKey, TValue>(this IEnumerableAsync<KeyValuePair<TKey, TValue>> enumerable)
        {
            return new DictionaryAsync<KeyValuePair<TKey, TValue>, TKey, TValue>(enumerable, 
                (kvp) => kvp.Key,
                (kvp) => kvp.Value);
        }

        public static IEnumerableAsync<TKey> SelectKeys<TKey, TValue>(this IEnumerableAsync<KeyValuePair<TKey, TValue>> enumerable)
        {
            return enumerable.Select(kvp => kvp.Key);
        }

        public static IEnumerableAsync<TValue> SelectValues<TKey, TValue>(this IEnumerableAsync<KeyValuePair<TKey, TValue>> enumerable)
        {
            return enumerable.Select(kvp => kvp.Value);
        }

    }
}

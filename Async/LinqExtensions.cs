using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Linq.Expressions;

namespace BlackBarLabs.Collections.Async
{
    public static class LinqExtensions
    {
        public static async Task ForAllAsync<T>(this IEnumerableAsync<T> items, T action)
        {
            var iterator = items.GetIterator();
            await iterator.IterateAsync(action);
            // while (await enumerator.MoveNextAsync(action)) { };
        }

        public static async Task<bool> FirstAsync<T>(this IEnumerableAsync<T> items, T action)
        {
            var enumerator = items.GetEnumerator();
            return await enumerator.MoveNextAsync(action);
        }

        public static IEnumerableAsync<T> TakeAsync<T>(this IEnumerableAsync<T> items, int limit)
        {
            YieldCallbackAsync<T> yieldAsync = async yield =>
            {
                using (var enumerator = items.GetEnumerator())
                {
                    while (limit > 0 && await enumerator.MoveNextAsync(yield))
                    {
                        limit--;
                    }
                }
            };
            return new EnumerableAsync<T>(yieldAsync);
        }

        public async static Task ForYield<T>(this IEnumerable<T> items, Func<T, Task> yieldAsync)
        {
            foreach(var item in items)
            {
                await yieldAsync.Invoke(item);
            }
        }

        public static IEnumerableAsync<Func<T, Task>> AsEnumerableAsync<T>(this IEnumerable<T> items)
        {
            return EnumerableAsync.YieldAsync<Func<T, Task>>(
                async (yieldAsync) =>
                {
                    await items.ForYield(yieldAsync);
                });
        }

        #region ToDictionary

        //public delegate void ToDictionaryDelegate<TKey, TValue>(TKey key, TValue value);
        //public static IDictionary<TKey, TValue> ToEnumerable<TDelegateItems, T1, T2, T3, TKey, TValue>(
        //    this IEnumerableAsync<TDelegateItems> items,
        //    Action<T1, T2, T3, ToDictionaryDelegate<TKey, TValue>> convert)
        //{
        //    var iterator = items.GetEnumerable<KeyValuePair<TKey, TValue>, Func<T1, T2, T3, KeyValuePair<TKey, TValue>>>(
        //        (t1, t2, t3) =>
        //        {
        //            var key = default(TKey);
        //            var value = default(TValue);
        //            convert(t1, t2, t3, (k, v) => { key = k; value = v; });
        //            return new KeyValuePair<TKey, TValue>(key, value);
        //        });
        //    return iterator.ToDictionary(;
        //}

        //public static IDictionary<TKey, TValue> ToDictionary<TDelegateItems, T1, TKey, TValue>(
        //    this IEnumerableAsync<TDelegateItems> items,
        //    Action<T1, ToDictionaryDelegate<TKey, TValue>> convert)
        //{
        //    var iterator = items.GetEnumerable<TResult, Func<T1, TResult>>(convert);
        //    return iterator;
        //}

        #endregion

        #region ToEnumerable

        public static IEnumerable<TResult> ToEnumerable<TDelegateItems, TDelegateConvert, TResult>(
            this IEnumerableAsync<TDelegateItems> items,
            TDelegateConvert convert)
        {
            var iterator = items.GetEnumerable<TResult, TDelegateConvert>(convert);
            return iterator;
        }
        
        public static IEnumerable<TResult> ToEnumerable<TDelegateItems, T1, TResult>(
            this IEnumerableAsync<TDelegateItems> items,
            Func<T1, TResult> convert)
        {
            var iterator = items.GetEnumerable<TResult, Func<T1, TResult>>(convert);
            return iterator;
        }

        public static IEnumerable<TResult> ToEnumerable<TDelegateItems, T1, T2, TResult>(
            this IEnumerableAsync<TDelegateItems> items,
            Func<T1, T2, TResult> convert)
        {
            var iterator = items.GetEnumerable<TResult, Func<T1, T2, TResult>>(convert);
            return iterator;
        }

        public static IEnumerable<TResult> ToEnumerable<TDelegateItems, T1, T2, T3, TResult>(
            this IEnumerableAsync<TDelegateItems> items,
            Func<T1, T2, T3, TResult> convert)
        {
            var iterator = items.GetEnumerable<TResult, Func<T1, T2, T3, TResult>>(convert);
            return iterator;
        }

        public static IEnumerable<TResult> ToEnumerable<TDelegateItems, T1, T2, T3, T4, TResult>(
            this IEnumerableAsync<TDelegateItems> items,
            Func<T1, T2, T3, T4, TResult> convert)
        {
            var iterator = items.GetEnumerable<TResult, Func<T1, T2, T3, T4, TResult>>(convert);
            return iterator;
        }

        public static IEnumerable<TResult> ToEnumerable<TDelegateItems, T1, T2, T3, T4, T5, T6, T7, TResult>(
            this IEnumerableAsync<TDelegateItems> items,
            Func<T1, T2, T3, T4, T5, T6, T7, TResult> convert)
        {
            var iterator = items.GetEnumerable<TResult, Func<T1, T2, T3, T4, T5, T6, T7, TResult>>(convert);
            return iterator;
        }

        public static IEnumerable<TResult> ToEnumerable<TDelegateItems, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TResult>(
            this IEnumerableAsync<TDelegateItems> items,
            Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TResult> convert)
        {
            var iterator = items.GetEnumerable<TResult, Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TResult>>(convert);
            return iterator;
        }

        #endregion

    }
}

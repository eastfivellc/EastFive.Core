using BlackBarLabs.Extensions;
using EastFive.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EastFive.Linq
{
    public static class ReduceExtensions
    {
        public static async Task<TSelect[]> SelectReduceAsync<TItem, TSelect>(this IEnumerable<TItem> items,
            Func<TItem, Func<TSelect, Task<TSelect[]>>, Func<Task<TSelect[]>>, Task<Task<TSelect[]>>> select)
        {
            return await items.SelectReduce<TItem, TSelect, Task<TSelect[]>>(
                    async (item, next, skip) => await await select(item,
                        next,
                        skip),
                    (selected) => selected.ToTask());
        }

        public static TSelect[] SelectReduce<TItem, TSelect>(this IEnumerable<TItem> items,
            Func<TItem, Func<TSelect, TSelect[]>, Func<TSelect[]>, TSelect[]> select)
        {
            return items.SelectReduce<TItem, TSelect, TSelect[]>(
                (item, next, skip) => select(item, next, skip),
                (values) => values.ToArray());
        }

        public static TResult SelectReduce<TItem, TSelect, TResult>(this IEnumerable<TItem> items,
            Func<TItem, Func<TSelect, TResult>, TResult> select,
            Func<TSelect[], TResult> reduce)
        {
            return items.SelectReduce(
                (item, next, skip) => select(item, next),
                reduce);
        }

        public static KeyValuePair<TSelect1, TSelect2>[] ZipReduce<TItem, TSelect1, TSelect2>(this IEnumerable<TItem> items,
            Func<
                TItem,
                Func<TSelect1, TSelect2, KeyValuePair<TSelect1, TSelect2>[]>,
                Func<KeyValuePair<TSelect1, TSelect2>[]>,
                KeyValuePair<TSelect1, TSelect2>[]> select)
        {
            return items.ZipReduce<TItem, TSelect1, TSelect2, KeyValuePair<TSelect1, TSelect2>[]>(
                select,
                kvps => kvps.ToArray());
        }

        public static TResult ZipReduce<TItem, TSelect1, TSelect2, TResult>(this IEnumerable<TItem> items,
            Func<
                TItem,
                Func<TSelect1, TSelect2, TResult>,
                Func<TResult>,
                TResult> select,
            Func<KeyValuePair<TSelect1, TSelect2>[], TResult> reduce)
        {
            return items.SelectReduce<TItem, KeyValuePair<TSelect1, TSelect2>, TResult>(
                (item, next, skip) => select(
                    item,
                    (k, p) => next(k.PairWithValue(p)),
                    skip),
                reduce);
        }
        
        public static Task<TResult> ZipReduceAsync<TItem, TSelect1, TSelect2, TResult>(this IEnumerable<TItem> items,
            Func<
                TItem,
                Func<TSelect1, TSelect2, Task<TResult>>,
                Func<Task<TResult>>,
                Task<TResult>> select,
            Func<KeyValuePair<TSelect1, TSelect2>[], TResult> reduce)
        {
            return items.SelectReduce<TItem, KeyValuePair<TSelect1, TSelect2>, Task<TResult>>(
                (item, next, skip) => select(
                    item,
                    (k, p) => next(k.PairWithValue(p)),
                    skip),
                (kvps) => reduce(kvps).ToTask());
        }

        public static TResult SelectReduce<TItem, TSelect, TResult>(this IEnumerable<TItem> items,
            Func<TItem, Func<TSelect, TResult>, Func<TResult>, TResult> select,
            Func<TSelect[], TResult> reduce)
        {
            var enumerator = items.GetEnumerator();
            return enumerator.SelectReduce(new TSelect[] { }, select,
                (rs) => reduce(rs.ToArray()));
        }

        private static TResult SelectReduce<TItem, TSelect, TResult>(this IEnumerator<TItem> items,
            TSelect[] selections,
            Func<TItem, Func<TSelect, TResult>, Func<TResult>, TResult> select,
            Func<TSelect[], TResult> reduce)
        {
            if (!items.MoveNext())
                return reduce(selections.ToArray());

            return select(items.Current,
                (r) =>
                {
                    return items.SelectReduce(selections.Append(r).ToArray(), select, reduce);
                },
                () =>
                {
                    return items.SelectReduce(selections, select, reduce);
                });
        }

        public static TResult Reduce<T1, T2, TItem, TResult>(this IEnumerable<TItem> items,
            TResult initial, T1 v1, T2 v2,
            Func<TResult, T1, T2, TItem, Func<T1, T2, TResult, TResult>, TResult> callback)
        {
            var enumerator = items.GetEnumerator();
            return enumerator.Reduce(initial, v1, v2, callback);
        }

        private static TResult Reduce<T1, T2, TItem, TResult>(this IEnumerator<TItem> items,
            TResult initial, T1 v1, T2 v2,
            Func<TResult, T1, T2, TItem, Func<T1, T2, TResult, TResult>, TResult> callback)
        {
            if (!items.MoveNext())
                return initial;

            return callback(initial, v1, v2, items.Current,
                (v1next, v2next, r) => items.Reduce(r, v1next, v2next, callback));
        }

        public static TResult Reduce<TItem, TResult>(this IEnumerable<TItem> items,
           Func<TItem, Func<TResult>, TResult> callback, 
           Func<TResult> onEmpty)
        {
            var enumerator = items.GetEnumerator();
            return enumerator.Reduce(callback, onEmpty);
        }

        private static TResult Reduce<TItem, TResult>(this IEnumerator<TItem> items,
            Func<TItem, Func<TResult>, TResult> callback,
            Func<TResult> onEmpty)
        {
            if (!items.MoveNext())
                return onEmpty();

            return callback(items.Current,
                () => items.Reduce(callback, onEmpty));
        }

        public static IEnumerable<TResult> ReduceItems<T1, TItem, TResult>(this IEnumerable<TItem> items,
            T1 v1,
            Func<
                T1, TItem,
                Func<TResult, T1, IEnumerable<TResult>>,  // next
                Func<T1, IEnumerable<TResult>>, // skip
                IEnumerable<TResult>> callback)
        {
            var enumerator = items.GetEnumerator();
            return enumerator.ReduceItems(v1, callback,
                (v1_, items_) => items_);
        }

        public static TResult ReduceItems<T1, TItem, TSelect, TResult>(this IEnumerable<TItem> items,
            T1 v1,
            Func<
                T1, TItem,
                Func<TSelect, T1, TResult>,  // next
                Func<T1, TResult>, // skip
                TResult> callback,
            Func<T1, IEnumerable<TSelect>, TResult> complete)
        {
            var enumerator = items.GetEnumerator();
            return enumerator.ReduceItems(v1, callback, complete);
        }

        private static TResult ReduceItems<T1, TItem, TSelect, TResult>(this IEnumerator<TItem> items,
            T1 v1,
            Func<
                T1, TItem,
                Func<TSelect, T1, TResult>,  // next
                Func<T1, TResult>, // skip
                TResult> callback,
            Func<T1, IEnumerable<TSelect>, TResult> complete)
        {
            if (!items.MoveNext())
                return complete(v1, new TSelect[] { });

            return callback(v1, items.Current,
                (r, v1next) => items.ReduceItems(v1next, callback, (v1_, items_) => complete(v1_, items_.Append(r))),
                (v1next) => items.ReduceItems(v1next, callback, complete));
        }

        //private static IEnumerable<TResult> ReduceItems<T1, TItem, TResult>(this IEnumerator<TItem> items,
        //    T1 v1,
        //    Func<
        //        T1, TItem,
        //        Func<TResult, T1, IEnumerable<TResult>>,  // next
        //        Func<T1, IEnumerable<TResult>>, // skip
        //        IEnumerable<TResult>> callback)
        //{
        //    if (!items.MoveNext())
        //        return new TResult[] { };

        //    return callback(v1, items.Current,
        //        (r, v1next) => items.ReduceItems(v1next, callback).Append(r),
        //        (v1next) => items.ReduceItems(v1next, callback));
        //}

        public static IEnumerable<TResult> ReduceItems<T1, T2, TItem, TResult>(this IEnumerable<TItem> items,
            T1 v1, T2 v2,
            Func<
                T1, T2, TItem,
                Func<T1, T2, TResult, IEnumerable<TResult>>,  // next
                Func<T1, T2, IEnumerable<TResult>>, // skip
                IEnumerable<TResult>> callback)
        {
            var enumerator = items.GetEnumerator();
            return enumerator.ReduceItems(v1, v2, callback);
        }

        private static IEnumerable<TResult> ReduceItems<T1, T2, TItem, TResult>(this IEnumerator<TItem> items,
            T1 v1, T2 v2,
            Func<
                T1, T2, TItem,
                Func<T1, T2, TResult, IEnumerable<TResult>>,  // next
                Func<T1, T2, IEnumerable<TResult>>, // skip
                IEnumerable<TResult>> callback)
        {
            if (!items.MoveNext())
                return new TResult[] { };

            return callback(v1, v2, items.Current,
                (v1next, v2next, r) => items.ReduceItems(v1next, v2next, callback).Append(r),
                (v1next, v2next) => items.ReduceItems(v1next, v2next, callback));
        }
    }
}

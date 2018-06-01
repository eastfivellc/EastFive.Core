﻿using BlackBarLabs.Extensions;
using EastFive.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EastFive.Linq
{
    public static class MapReduceExtensions
    {
        public static TResult FlatMap<TItem, TSelect, TResult>(this IEnumerable<TItem> items,
            Func<TItem, Func<TSelect, TResult>, TResult> select,
            Func<IEnumerable<TSelect>, TResult> reduce)
        {
            return items.FlatMap(
                (item, next, skip) => select(item, next),
                reduce);
        }

        public static TResult FlatMap<TItem, TSelect, TResult>(this IEnumerable<TItem> items,
            Func<
                TItem,
                Func<TSelect, TResult>,  // next
                Func<TResult>, // skip
                TResult> callback,
            Func<IEnumerable<TSelect>, TResult> complete)
        {
            return items.FlatMap<TItem, int, TSelect, TResult>(
                1,
                (item, t1, next, skip) => callback(item,
                    (select) => next(select, t1),
                    () => skip(t1)),
                (selections, t1) => complete(selections));
        }

        public static TResult FlatMap<TItem, T1, TSelect, TResult>(this IEnumerable<TItem> items,
                T1 item1,
            Func<
                TItem, T1,
                Func<TSelect, T1, TResult>,  // next
                Func<T1, TResult>, // skip
                TResult> callback,
            Func<TSelect[], T1, TResult> complete)
        {
            if (typeof(TResult).IsGenericType && typeof(TResult).GetGenericTypeDefinition() == typeof(Task<>))
            {
                var method = typeof(MapReduceExtensions).GetMethod("FlatMapGenericAsync", BindingFlags.NonPublic | BindingFlags.Static);
                var generic = method.MakeGenericMethod(typeof(TItem), typeof(T1), typeof(TSelect), typeof(TResult).GenericTypeArguments.First());
                var r = generic.Invoke(null, new object[] { items, item1, callback, complete});
                var tr = (TResult)r;
                return tr;
            }

            return items
                .Aggregate(
                    item1.PairWithValue(new TSelect[] { }),
                    (aggr, item) =>
                    {
                        var block = new ManualResetEvent(false);
                        var resultToDiscard = callback(
                            item, aggr.Key,
                            (selection, item1Next) =>
                            {
                                aggr = item1Next.PairWithValue(aggr.Value.Append(selection).ToArray());
                                block.Set();
                                return default(TResult);
                            },
                            (item1Next) =>
                            {
                                aggr = item1Next.PairWithValue(aggr.Value);
                                block.Set();
                                return default(TResult);
                            });
                        block.WaitOne();
                        return aggr;
                    },
                    aggr => complete(aggr.Value, aggr.Key));
        }

        public static Task<object> FlatMapAsync<TItem, T1, TSelect>(this IEnumerable<TItem> items,
                T1 item1,
            Func<
                TItem, T1,
                Func<TSelect, T1, Task<object>>,  // next
                Func<T1, Task<object>>, // skip
                Task<object>> callback,
            Func<TSelect[], T1, Task<object>> complete)
        {
            var globalSelection = new TSelect[] { };
            
            return items.Aggregate(
                (new ManualResetEvent(true)).PairWithValue(default(object).ToTask()).ToTask(),
                async (blockAndTaskAsync, item) =>
                {
                    var blockAndTask = await blockAndTaskAsync;
                    var block = new ManualResetEvent(false);
                    blockAndTask.Key.WaitOne(); // wait here so Item1 is updated

                    var completeBlock = new ManualResetEvent(false);
                    var nextTask = default(Task<object>);
                    var completeCallback = Task<object>.Run(
                        () =>
                        {
                            completeBlock.WaitOne();
                            return nextTask;
                        });
                    
                    nextTask = await blockAndTask.Value.ContinueWith(
                        (lastTask) =>
                        {
                            return callback(
                                item, item1,
                                (selection, item1Next) =>
                                {
                                    globalSelection = globalSelection.Append(selection).ToArray();
                                    item1 = item1Next;
                                    block.Set();
                                    return completeCallback;
                                },
                                (item1Next) =>
                                {
                                    item1 = item1Next;
                                    block.Set();
                                    return completeCallback;
                                });
                        });
                    completeBlock.Set();
                    return block.PairWithValue(nextTask);
                },
                async (blockAndTaskAsync) =>
                {
                    var blockAndTask = await blockAndTaskAsync;
                    blockAndTask.Key.WaitOne(); // wait here so Item1 is updated
                    await blockAndTask.Value;
                    return await complete(globalSelection, item1);
                });
        }

        /// <summary>
        /// This method is tail optimized mean that calling methods should not use the return values from callback's next/skip
        /// </summary>
        /// <typeparam name="TItem"></typeparam>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="TSelect"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="items"></param>
        /// <param name="item1"></param>
        /// <param name="callback"></param>
        /// <param name="complete"></param>
        /// <returns></returns>
        private static async Task<TResult> FlatMapGenericAsync<TItem, T1, TSelect, TResult>(this IEnumerable<TItem> items,
                T1 item1,
            Func<
                TItem, T1,
                Func<TSelect, T1, Task<TResult>>,  // next
                Func<T1, Task<TResult>>, // skip
                Task<TResult>> callback,
            Func<TSelect[], T1, Task<TResult>> complete)
        {
            var block = new ManualResetEvent(false);
            var selections = new TSelect[] { };
            var itemHistory = new T1[] { item1 };

            var callComplete = Task<TResult>.Run(
                () =>
                {
                    block.WaitOne();
                    return complete(selections, itemHistory[0]);
                });

            var blankSpace = default(TResult).ToTask();
            return await await items
                .Aggregate(
                    blankSpace,
                    async (aggr, item) =>
                    {
                        return await await aggr.ContinueWith(
                            async last =>
                            {
                                return await callback(
                                    item, itemHistory[0],
                                    (selection, item1Next) =>
                                    {
                                        itemHistory[0] = item1Next;
                                        selections = selections.Append(selection).ToArray();
                                        return blankSpace;
                                    },
                                    (item1Next) =>
                                    {
                                        itemHistory[0] = item1Next;
                                        return blankSpace;
                                    });
                            });
                    })
                .ContinueWith(
                    final =>
                    {
                        block.Set();
                        return callComplete;
                    });
        }
    }
}
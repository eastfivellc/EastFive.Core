using BlackBarLabs.Extensions;
using EastFive.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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

        private class Reduction<TSelect1, TSelect2, TResult>
        {
            internal ManualResetEvent selected;
            internal ManualResetEvent reduced;
            internal ManualResetEvent trigger;
            internal TSelect1[] p1s;
            internal TSelect2[] p2s;
            internal TResult result;
        }

        public static TResult SelectPartition<TItem, TSelect1, TSelect2, TResult>(this IEnumerable<TItem> items,
            Func<TItem, Func<TSelect1, TResult>, Func<TSelect2, TResult>, TResult> select,
            Func<TSelect1[], TSelect2[], TResult> reduce,
            int depthLimit = 100)
        {
            return items
                .Split(index => depthLimit)
                .Aggregate(
                    new Reduction<TSelect1, TSelect2, TResult>[] { },
                    (Reduction<TSelect1, TSelect2, TResult>[] reductions, IEnumerable<TItem> split) =>
                    {
                        var reduction = new Reduction<TSelect1, TSelect2, TResult>
                        {
                            selected = new ManualResetEvent(false),
                            reduced = new ManualResetEvent(false),
                            trigger = new ManualResetEvent(false),
                        };
                        var thread = new Thread(() =>
                        {
                            var enumerator = split.GetEnumerator();
                            reduction.result = enumerator.SelectPartition(new TSelect1[] { }, new TSelect2[] { }, select,
                                (p1s, p2s) =>
                                {
                                    reduction.p1s = p1s;
                                    reduction.p2s = p2s;
                                    reduction.selected.Set();
                                    reduction.trigger.WaitOne();
                                    return reduction.result;
                                });
                            reduction.reduced.Set();
                        });
                        thread.Start();
                        return reductions.Append(reduction).ToArray();
                    },
                    (Reduction<TSelect1, TSelect2, TResult>[] reductions) =>
                    {
                        var allComplete = reductions.All(reduction => reduction.selected.WaitOne());
                        var p1s = reductions.SelectMany(reduction => reduction.p1s.NullToEmpty()).ToArray();
                        var p2s = reductions.SelectMany(reduction => reduction.p2s.NullToEmpty()).ToArray();
                        return reductions
                            .Reverse()
                            .Aggregate(
                                reduce(p1s, p2s),
                                (result, reduction) =>
                                {
                                    reduction.result = result;
                                    reduction.trigger.Set();
                                    reduction.reduced.WaitOne();
                                    return reduction.result;
                                });
                    });
        }

        //public static TResult SelectPartition<TItem, TSelect1, TSelect2, TResult>(this IEnumerable<TItem> items,
        //    Func<TItem, Func<TSelect1, TResult>, Func<TSelect2, TResult>, TResult> select,
        //    Func<TSelect1[], TSelect2[], TResult> reduce)
        //{
        //    var splits = items
        //        .Skip(10)
        //        .Split(index => 10)
        //        .Aggregate(
        //            new Task<TResult>(
        //                () =>
        //                {
        //                    var enumerator = items.Take(10).GetEnumerator();
        //                    return enumerator.SelectPartition(new TSelect1[] { }, new TSelect2[] { }, 
        //                        (item, next, skip) => ((item, next, skip) =>  select(item, next, skip)),
        //                        (rs1, rs2) => rs1.PairWithValue(rs2));
        //                }),
        //            (lastTask, split) =>
        //            {

        //                lastTask.ContinueWith(
        //                    (finishedTask) =>
        //                    {

        //                        finishedTask.R
        //                    })
        //            })
        //    return splits.Aggregate(
        //        (new TSelect1[] { }).PairWithValue(new TSelect2[] { }),
        //        (aggr, split) =>
        //        {
        //            var enumerator = split.GetEnumerator();
        //            return enumerator.SelectPartition(new TSelect1[] { }, new TSelect2[] { }, select,
        //                (rs1, rs2) => aggr.Key.Concat(rs1).ToArray().PairWithValue(aggr.Value.Concat(rs2).ToArray());
        //        },
        //        kvps =>
        //        {

        //        });
        //}

        private static TResult SelectPartition<TItem, TSelect1, TSelect2, TResult>(this IEnumerator<TItem> items,
            TSelect1[] selections1, TSelect2[] selections2,
            Func<TItem, Func<TSelect1, TResult>, Func<TSelect2, TResult>, TResult> select,
            Func<TSelect1[], TSelect2[], TResult> reduce)
        {
            if (!items.MoveNext())
                return reduce(selections1, selections2);

            return select(items.Current,
                (r) =>
                {
                    return items.SelectPartition(selections1.Append(r).ToArray(), selections2, select, reduce);
                },
                (r) =>
                {
                    return items.SelectPartition(selections1, selections2.Append(r).ToArray(), select, reduce);
                });
        }

        //public static TResult SelectPartition<TItem, TSelect1, TSelect2, TResult>(this IEnumerable<TItem> items,
        //    Func<TItem, Func<TSelect1, TResult>, Func<TSelect2, TResult>, TResult> select,
        //    Func<TSelect1[], TSelect2[], TResult> reduce)
        //{
        //    var enumerator = items.GetEnumerator();
        //    return enumerator.SelectPartition(new TSelect1[] { }, new TSelect2[] { }, select,
        //        (rs1, rs2) => reduce(rs1, rs2));
        //}

        //private static TResult SelectPartition<TItem, TSelect1, TSelect2, TResult>(this IEnumerator<TItem> items,
        //    TSelect1[] selections1, TSelect2[] selections2,
        //    Func<TItem, Func<TSelect1, TResult>, Func<TSelect2, TResult>, TResult> select,
        //    Func<TSelect1[], TSelect2[], TResult> reduce)
        //{
        //    if (!items.MoveNext())
        //        return reduce(selections1, selections2);

        //    return select(items.Current,
        //        (r) =>
        //        {
        //            return items.SelectPartition(selections1.Append(r).ToArray(), selections2, select, reduce);
        //        },
        //        (r) =>
        //        {
        //            return items.SelectPartition(selections1, selections2.Append(r).ToArray(), select, reduce);
        //        });
        //}

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

        private struct ReduceItemStep<TSelection>
        {
            public ManualResetEvent trigger;
            public TSelection selection;
        }

        public static TResult ReduceItems<TItem, TSelect, TResult>(this IEnumerable<TItem> items,
            Func<
                TItem,
                Func<TSelect, TResult>,  // next
                Func<TResult>, // skip
                TResult> callback,
            Func<TSelect[], TResult> complete)
        {
            var block = new ManualResetEvent(false);
            TResult result = default(TResult);

            var steps = items
                .Select(
                    item =>
                    {
                        var step = new ReduceItemStep<TSelect>
                        {
                            trigger = new ManualResetEvent(false),
                        };
                        callback(
                            item,
                            (selection) =>
                            {
                                step.selection = selection;
                                step.trigger.Set();
                                block.WaitOne();
                                return result;
                            },
                            () =>
                            {
                                step.trigger.Set();
                                block.WaitOne();
                                return result;
                            });
                        return step;
                    })
                .Select(step =>
                    {
                        step.trigger.WaitOne();
                        return step.selection;
                    });
            result = complete(steps.ToArray());
            block.Set();
            return result;
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
            TResult result = default(TResult);
            var block = new ManualResetEvent(false);
            
            return items
                .Aggregate(
                    v1.PairWithValue(new TSelect[] { }),
                    (valueKvp, item) =>
                    {
                        T1 vNext = default(T1);
                        var selected = valueKvp.Value;
                        var trigger = new ManualResetEvent(false);
                        var task = new Task(
                            () => callback(
                                valueKvp.Key,
                                item,
                                (selection, valueNext) =>
                                {
                                    vNext = valueNext;
                                    selected = selected.Append(selection).ToArray();
                                    trigger.Set();
                                    block.WaitOne();
                                    return result;
                                },
                                (valueNext) =>
                                {
                                    vNext = valueNext;
                                    trigger.Set();
                                    block.WaitOne();
                                    return result;
                                }));
                        task.Start();
                        trigger.WaitOne();
                        return vNext.PairWithValue(selected);
                    },
                    final =>
                    {
                        result = complete(final.Key, final.Value);
                        block.Set();
                        return result;
                    });
        }

        public static TResult ReduceItemsX<T1, TItem, TSelect, TResult>(this IEnumerable<TItem> items,
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

        public static TResult First<TITem, TResult>(this IEnumerable<TITem> items,
            Func<TITem, Func<TResult>, TResult> next,
            Func<TResult> onNotFound)
        {
            var enumerator = items.GetEnumerator();
            return enumerator.First(next, onNotFound);
        }

        public static TResult First<TITem, TResult>(this IEnumerator<TITem> items,
            Func<TITem, Func<TResult>, TResult> next,
            Func<TResult> onNotFound)
        {
            if (!items.MoveNext())
                return onNotFound();

            return next(items.Current, () => items.First(next, onNotFound));
        }
    }
}

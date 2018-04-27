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
    public static class SelectPartitionExtensions
    {
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
        
    }
}

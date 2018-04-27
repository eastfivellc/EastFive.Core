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
    public static class ReduceExtensionsCore
    {
        //private static TResult SelectSubset<TItem, TSelect, TResult>(this IEnumerable<TItem> items,
        //    Func<TItem, Func<TSelect, TResult>, Func<TResult>, TResult> select,
        //    Func<TSelect[], TResult> reduce)
        //{
        //    var enumerable = items.GetEnumerator();
        //    var selections = enumerable.SelectSubset(select);
        //    return reduce(selections);
        //}

        //private static IEnumerable<TSelect> SelectSubset<TItem, TSelect>(this IEnumerator<TItem> items,
        //    Func<TItem, Func<TSelect, IEnumerable<TSelect>>, Func<IEnumerable<TSelect>>, IEnumerable<TSelect>> select,
        //    ManualResetEvent trigger)
        //{
        //    if (!items.MoveNext())
        //        yield break;

        //    foreach(var item in select(items.Current,
        //        (r) =>
        //        {
        //            return items.SelectSubset(select);
        //        },
        //        () =>
        //        {
        //            return items.SelectSubset(selections, select, reduce);
        //        }))
        //    {
        //        yield return item;
        //    }
        //}
        

        //private struct ReduceItemStep<TSelection>
        //{
        //    public ManualResetEvent trigger;
        //    public TSelection selection;
        //}

        //public static TResult Reduce<TItem, TSelect, TResult>(this IEnumerable<TItem> items,
        //    Func<
        //        TItem,
        //        Func<TSelect, TResult>,  // next
        //        Func<TResult>, // skip
        //        TResult> callback,
        //    Func<TSelect[], TResult> complete)
        //{
        //    var block = new ManualResetEvent(false);
        //    TResult result = default(TResult);

        //    var steps = items
        //        .Select(
        //            item =>
        //            {
        //                var step = new ReduceItemStep<TSelect>
        //                {
        //                    trigger = new ManualResetEvent(false),
        //                };
        //                callback(
        //                    item,
        //                    (selection) =>
        //                    {
        //                        step.selection = selection;
        //                        step.trigger.Set();
        //                        block.WaitOne();
        //                        return result;
        //                    },
        //                    () =>
        //                    {
        //                        step.trigger.Set();
        //                        block.WaitOne();
        //                        return result;
        //                    });
        //                return step;
        //            })
        //        .Select(step =>
        //            {
        //                step.trigger.WaitOne();
        //                return step.selection;
        //            });
        //    result = complete(steps.ToArray());
        //    block.Set();
        //    return result;
        //}

        //public static TResult Reduce<T1, TItem, TSelect, TResult>(this IEnumerable<TItem> items,
        //    T1 v1,
        //    Func<
        //        T1, TItem,
        //        Func<TSelect, T1, TResult>,  // next
        //        Func<T1, TResult>, // skip
        //        TResult> callback,
        //    Func<T1, IEnumerable<TSelect>, TResult> complete)
        //{
        //    TResult result = default(TResult);
        //    var block = new ManualResetEvent(false);
            
        //    return items
        //        .Aggregate(
        //            v1.PairWithValue(new TSelect[] { }),
        //            (valueKvp, item) =>
        //            {
        //                T1 vNext = default(T1);
        //                var selected = valueKvp.Value;
        //                var trigger = new ManualResetEvent(false);
        //                var task = new Task(
        //                    () => callback(
        //                        valueKvp.Key,
        //                        item,
        //                        (selection, valueNext) =>
        //                        {
        //                            vNext = valueNext;
        //                            selected = selected.Append(selection).ToArray();
        //                            trigger.Set();
        //                            block.WaitOne();
        //                            return result;
        //                        },
        //                        (valueNext) =>
        //                        {
        //                            vNext = valueNext;
        //                            trigger.Set();
        //                            block.WaitOne();
        //                            return result;
        //                        }));
        //                task.Start();
        //                trigger.WaitOne();
        //                return vNext.PairWithValue(selected);
        //            },
        //            final =>
        //            {
        //                result = complete(final.Key, final.Value);
        //                block.Set();
        //                return result;
        //            });
        //}
    }
}

using BlackBarLabs;
using BlackBarLabs.Extensions;
using EastFive.Analytics;
using EastFive.Collections.Generic;
using EastFive.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EastFive.Linq.Async
{
    public static partial class EnumerableAsync
    {
        public static IEnumerableAsync<T> Where<T>(this IEnumerableAsync<T> enumerable, Func<T, bool> predicate)
        {
            var enumeratorAsync = enumerable.GetEnumerator();
            return Yield<T>(
                   async (yieldReturn, yieldBreak) =>
                   {
                       if (!await enumeratorAsync.MoveNextAsync())
                           return yieldBreak;
                       var current = enumeratorAsync.Current;
                       while (!predicate(current))
                       {
                           if (!await enumeratorAsync.MoveNextAsync())
                               return yieldBreak;
                           current = enumeratorAsync.Current;
                       }
                       return yieldReturn(current);
                   });
        }

        public static IEnumerableAsync<T> AsyncWhere<T>(this IEnumerableAsync<T> enumerable, Func<T, Task<bool>> predicate)
        {
            var enumeratorAsync = enumerable.GetEnumerator();
            return Yield<T>(
                   async (yieldReturn, yieldBreak) =>
                   {
                       if (!await enumeratorAsync.MoveNextAsync())
                           return yieldBreak;
                       var current = enumeratorAsync.Current;
                       while (!await predicate(current))
                       {
                           if (!await enumeratorAsync.MoveNextAsync())
                               return yieldBreak;
                           current = enumeratorAsync.Current;
                       }
                       return yieldReturn(current);
                   });
        }

        public static IEnumerableAsync<T> AsyncWhere<T>(this IEnumerable<T> items, Func<T, Task<bool>> predicate)
        {
            var enumerator = items.GetEnumerator();
            return Yield<T>(
                async (yieldReturn, yieldBreak) =>
                {
                    while (enumerator.MoveNext())
                    {
                        var item = enumerator.Current;
                        var match = await predicate(item);
                        if (match)
                            return yieldReturn(item);
                    }
                    return yieldBreak;
                });
        }

        public static IEnumerableAsync<TResult> Select<T, TResult>(this IEnumerableAsync<T> enumerable, Func<T, TResult> selection,
            ILogger log = default(ILogger))
        {
            var selectId = Guid.NewGuid();
            var logSelect = log.CreateScope($"Select[{selectId}]");
            var eventId = 1;
            var enumeratorAsync = enumerable.GetEnumerator();
            return Yield<TResult>(
                async (yieldReturn, yieldBreak) =>
                {
                    var logItem = logSelect.CreateScope($"Item[{eventId++}]");
                    logItem.Trace($"START BLOCK");

                    if (enumeratorAsync.IsDefaultOrNull())
                        return yieldBreak;

                    logItem.Trace($"Calling MoveNextAsync.");
                    if (!await enumeratorAsync.MoveNextAsync())
                    {
                        enumeratorAsync = default(IEnumeratorAsync<T>);
                        logItem.Trace($"COMPLETE");
                        return yieldBreak;
                    }
                    var current = enumeratorAsync.Current;

                    logItem.Trace($"Begin transform");
                    var next = selection(current);
                    logItem.Trace($"Ended transform");

                    var result = yieldReturn(next);
                    logItem.Trace($"END BLOCK");
                    return result;
                });
        }

        //public static IEnumerableAsync<TResult> Select<T, TResult>(this IEnumerableAsync<T> enumerable, Func<T, TResult> selection,
        //    ILogger log = default(ILogger))
        //{
        //    var selectId = Guid.NewGuid();
        //    var logSelect = log.CreateScope($"Select[{selectId}]");
        //    var eventId = 1;
        //    var selected = new DelegateEnumerableAsync<TResult, T>(enumerable,
        //        async (enumeratorAsync, enumeratorDestination, moved, ended) =>
        //        {
        //            var logItem = logSelect.CreateScope($"Item[{eventId++}]");
        //            logItem.Trace($"START BLOCK");
        //            if (enumeratorAsync.IsDefaultOrNull())
        //                return ended();

        //            if(!logItem.IsDefaultOrNull())
        //                logItem.Trace($"Calling MoveNextAsync.");
        //            if (!await enumeratorAsync.MoveNextAsync())
        //            {
        //                logItem.Trace($"COMPLETE");
        //                return ended();
        //            }
        //            var current = enumeratorAsync.Current;

        //            logItem.Trace($"Begin transform");
        //            var next = selection(current);
        //            logItem.Trace($"Ended transform");

        //            var result = moved(next);
        //            logItem.Trace($"END BLOCK");
        //            return result;
        //        });

        //    return selected;
        //}

        public static IEnumerableAsync<TResult> SelectWithIndex<T, TResult>(this IEnumerableAsync<T> enumerable,
            Func<T, int, TResult> selection,
            ILogger log = default(ILogger))
        {
            var selectId = Guid.NewGuid();
            var logSelect = log.CreateScope($"Select[{selectId}]");
            var eventId = 0;
            var enumeratorAsync = enumerable.GetEnumerator();
            return EnumerableAsync.Yield<TResult>(
                async (moved, ended) =>
                {
                    var index = eventId;
                    var logItem = logSelect.CreateScope($"Item[{eventId++}]");
                    logItem.Trace($"START BLOCK");
                    if (enumeratorAsync.IsDefaultOrNull())
                        return ended;

                    if (!logItem.IsDefaultOrNull())
                        logItem.Trace($"Calling MoveNextAsync.");
                    if (!await enumeratorAsync.MoveNextAsync())
                    {
                        logItem.Trace($"COMPLETE");
                        return ended;
                    }
                    var current = enumeratorAsync.Current;

                    logItem.Trace($"Begin transform");
                    var next = selection(current, index);
                    logItem.Trace($"Ended transform");

                    var result = moved(next);
                    logItem.Trace($"END BLOCK");
                    return result;
                });
        }

        public static IEnumerableAsync<T> SelectWhereHasValue<T>(this IEnumerableAsync<Nullable<T>> enumerable)
            where T : struct
        {
            return enumerable
                .Where(valueMaybe => valueMaybe.HasValue)
                .Select(valueMaybe => valueMaybe.Value);
        }


        public static async Task<long> CountAsync<T>(this IEnumerableAsync<T> enumerable)
        {
            var enumerator = enumerable.GetEnumerator();
            int count = 0;
            while (await enumerator.MoveNextAsync())
            {
                count = count + 1;
            }
            return count;
        }

        public static async Task<TResult> FirstAsync<T, TResult>(this IEnumerableAsync<T> enumerable,
            Func<T, TResult> onOne,
            Func<TResult> onNone)
        {
            var enumerator = enumerable.GetEnumerator();
            if (await enumerator.MoveNextAsync())
                return onOne(enumerator.Current);
            return onNone();
        }

        public interface IFirstMatchResult<T>
        {
            bool Matched { get; }
            T Result { get; }
        }

        private class FirstMatchResultMatch<T> : IFirstMatchResult<T>
        {
            public bool Matched { get { return true; } }

            public T Result { get; set; }
        }

        private class FirstMatchResultNext<T> : IFirstMatchResult<T>
        {
            public bool Matched { get { return false; } }

            public T Result => throw new NotImplementedException();
        }

        public static async Task<TResult> FirstMatchAsync<T, TResult>(this IEnumerableAsync<T> enumerable,
            Func<T, Func<TResult, IFirstMatchResult<TResult>>, Func<IFirstMatchResult<TResult>>, IFirstMatchResult<TResult>> onOne,
            Func<TResult> onNone)
        {
            var enumerator = enumerable.GetEnumerator();
            while (await enumerator.MoveNextAsync())
            {
                var item = enumerator.Current;
                var oneResult = onOne(item,
                    (value) => new FirstMatchResultMatch<TResult>
                    {
                        Result = value,
                    },
                    () =>
                    {
                        return new FirstMatchResultNext<TResult>();
                    });
                if (oneResult.Matched)
                    return oneResult.Result;
            }
            return onNone();
        }

        public static Task<TResult> FirstAsyncMatchAsync<T, TResult>(this IEnumerableAsync<T> enumerable,
            Func<T, Func<Task<TResult>>, Task<TResult>> onOne,
            Func<TResult> onNone)
        {
            var enumerator = enumerable.GetEnumerator();
            return FirstAsyncInnerAsync(enumerator, onOne, onNone);
        }

        private static Task<TResult> FirstAsyncInnerAsync<T, TResult>(IEnumeratorAsync<T> enumerator,
            Func<T, Func<Task<TResult>>, Task<TResult>> onOne,
            Func<TResult> onNone)
        {
            Func<Task<TResult>>[] doNexts = new Func<Task<TResult>>[1];
            doNexts[0] =
                async () =>
                {
                    if (!await enumerator.MoveNextAsync())
                        return onNone();
                    return await onOne(enumerator.Current,
                        doNexts[0]);
                };
            return doNexts[0]();
        }

        public static async Task<TResult> LastAsync<T, TResult>(this IEnumerableAsync<T> enumerable,
            Func<T, TResult> onOne,
            Func<TResult> onNone)
        {
            var enumerator = enumerable.GetEnumerator();
            bool found = false;
            var item = default(T);
            while (await enumerator.MoveNextAsync())
            {
                found = true;
                item = enumerator.Current;
            }
            if (!found)
                return onNone();
            return onOne(item);
        }

        public static IEnumerableAsync<T> Empty<T>()
        {
            return EnumerableAsync.Yield<T>(
                (yeildReturn, yieldBreak) =>
                {
                    return yieldBreak.AsTask();
                });
        }

        public static async Task<IOrderedEnumerable<TItem>> OrderByAsync<TItem, TRank>(this IEnumerableAsync<TItem> enumerable,
            Func<TItem, TRank> ranking)
        {
            var items = await enumerable.Async();
            return items.OrderBy(ranking);
        }

        public static async Task<IOrderedEnumerable<TItem>> OrderByDescendingAsync<TItem, TRank>(this IEnumerableAsync<TItem> enumerable,
            Func<TItem, TRank> ranking)
        {
            var items = await enumerable.Async();
            return items.OrderByDescending(ranking);
        }

        public static async Task<TResult> SelectOneAsync<TItem, TResult>(this IEnumerableAsync<TItem> enumerable,
                Func<TItem, TItem, TItem> select,
            Func<TItem, TResult> onOne,
            Func<TResult> onNone)
        {
            var enumerator = enumerable.GetEnumerator();
            if (!await enumerator.MoveNextAsync())
                return onNone();
            var selected = enumerator.Current;
            while(await enumerator.MoveNextAsync())
            {
                var challenger = enumerator.Current;
                selected = select(selected, challenger);
            }
            return onOne(selected);
        }

        public static IEnumerableAsync<T> Take<T>(this IEnumerableAsync<T> enumerable, int count)
        {
            var enumerator = enumerable.GetEnumerator();
            var countdown = count+1;
            return EnumerableAsync.Yield<T>(
                async (yieldCont, yieldBreak) =>
                {
                    countdown--;
                    if (countdown <= 0)
                        return yieldBreak;
                    if (await enumerator.MoveNextAsync())
                        return yieldCont(enumerator.Current);
                    return yieldBreak;
                });
        }

        public static IEnumerableAsync<T> Skip<T>(this IEnumerableAsync<T> enumerable, int count)
        {
            var enumerator = enumerable.GetEnumerator();
            var countdown = count;
            return EnumerableAsync.Yield<T>(
                async (yieldCont, yieldBreak) =>
                {
                    while (countdown > 0)
                    {
                        if (!await enumerator.MoveNextAsync())
                            return yieldBreak;
                        countdown--;
                    }
                    if (!await enumerator.MoveNextAsync())
                        return yieldBreak;
                    return yieldCont(enumerator.Current);
                });
        }

        public static IEnumerableAsync<T> Distinct<T>(this IEnumerableAsync<T> enumerable)
        {
            var accumulation = new HashSet<T>(); // TODO: Should be a hash
            var enumerator = enumerable.GetEnumerator();
            return Yield<T>(
                async (yieldAsync, yieldBreak) =>
                {
                    while (true)
                    {
                        if (!await enumerator.MoveNextAsync())
                            return yieldBreak;

                        var current = enumerator.Current;
                        if (accumulation.Contains(current))
                            continue;

                        accumulation.Add(current);
                        return yieldAsync(current);
                    }
                });
        }

        public static IEnumerableAsync<T> Distinct<T, TKey>(this IEnumerableAsync<T> enumerable, Func<T, TKey> selectKey)
        {
            var accumulation = new HashSet<TKey>();
            var enumerator = enumerable.GetEnumerator();
            return Yield<T>(
                async (yieldAsync, yieldBreak) =>
                {
                    while (true)
                    {
                        if (!await enumerator.MoveNextAsync())
                            return yieldBreak;
                        var current = enumerator.Current;

                        var currentKey = selectKey(current);
                        if (accumulation.Contains(currentKey))
                            continue;
                        accumulation.Add(currentKey);
                        
                        return yieldAsync(current);
                    }
                });
        }

        public static IEnumerableAsync<T[]> Segments<T>(this IEnumerableAsync<T> enumerable, int segmentSize)
        {
            var segmentIndex = 0;
            var segment = new T[segmentSize];
            var enumerator = enumerable.GetEnumerator();
            return Yield<T[]>(
                async (yieldReturn, yieldBreak) =>
                {
                    // Works with nullification below to prevent double failed moveNexts
                    if (segment.IsDefaultOrNull())
                        return yieldBreak;

                    while (segmentIndex < segmentSize)
                    {
                        if (!await enumerator.MoveNextAsync())
                        {
                            var subseg = segment.Take(segmentIndex).ToArray();
                            segment = null; // Ensure failed MoveNext is only called once.
                            if (!subseg.Any())
                                return yieldBreak;

                            segmentIndex = 0;
                            return yieldReturn(subseg);
                        }
                        segment[segmentIndex] = enumerator.Current;
                    }
                    segmentIndex++;
                    return yieldReturn(segment);
                });
        }

        private static Task BatchAsync<T>(this IEnumerableAsync<T> enumerable,
            List<T> cache, EventWaitHandle moved, EventWaitHandle complete,
            EastFive.Analytics.ILogger diagnosticsTag = default(EastFive.Analytics.ILogger))
        {
            var enumerator = enumerable.GetEnumerator();

            Func<Task<bool>> getMoveNext =
                diagnosticsTag.IsDefaultOrNull()?
                    (Func<Task<bool>>)(() => enumerator.MoveNextAsync())
                    :
                    (Func<Task<bool>>)(async () =>
                    {
                        diagnosticsTag.Trace($"Moving");
                        var success = await enumerator.MoveNextAsync();
                        diagnosticsTag.Trace($"Moved");
                        return success;
                    });

            return Task.Run(CycleAsync);

            async Task CycleAsync()
            {
                try
                {
                    while (await getMoveNext())
                    {
                        diagnosticsTag.Trace($"Adding Value");
                        lock (cache)
                        {
                            cache.Add(enumerator.Current);
                            moved.Set();
                        }
                        diagnosticsTag.Trace($"Added Value");
                    }
                }
                catch (Exception ex)
                {
                    diagnosticsTag.Trace($"Captured exception:{ex.Message}");
                    throw;
                }
                finally
                {
                    complete.Set();
                    moved.Set();
                    diagnosticsTag.Trace($"Completed");
                }
            }
        }

        public static IEnumerableAsync<T[]> Batch<T>(this IEnumerableAsync<T> enumerable,
            EastFive.Analytics.ILogger diagnosticsTag = default(EastFive.Analytics.ILogger))
        {
            var segment = new List<T>();
            var moved = new AutoResetEvent(false);
            var complete = new ManualResetEvent(false);
            var taskComplete = false; // Prevents call to WaitOne if task has already completed
            var segmentTask = enumerable.BatchAsync(segment, moved, complete, diagnosticsTag);
            return Yield<T[]>(
                async (yieldReturn, yieldBreak) =>
                {
                    bool Complete()
                    {
                        if (taskComplete)
                            return true;

                        var taskIsFinished = complete.WaitOne(0);
                        if (taskIsFinished)
                        {
                            taskComplete = true; // prevent hanging on complete.WaitOne
                            return true;
                        }

                        return false;
                    }

                    while (!Complete())
                    {
                        moved.WaitOne();
                        T[] nextSegment;
                        lock (segment)
                        {
                            nextSegment = segment.ToArray();
                            segment.Clear();
                        }
                        if (nextSegment.Any())
                            return yieldReturn(nextSegment);
                        continue;
                    }

                    await segmentTask;
                    T[] lastSegment;
                    lock (segment)
                    {
                        lastSegment = segment.ToArray();
                        segment.Clear();
                    }
                    if (lastSegment.Any())
                        return yieldReturn(lastSegment);
                    return yieldBreak;
                });
        }

        public static IEnumerableAsync<T> Parallel<T>(this IEnumerableAsync<Task<T>> enumerable,
            bool maintainOrder = false,
            ILogger diagnostics = default(ILogger))
        {
            var segment = new List<Task<T>>();
            var moved = new AutoResetEvent(false);
            var complete = new ManualResetEvent(false);
            var segmentTask = enumerable.BatchAsync(segment, moved, complete, diagnostics);
            var taskComplete = false; // Prevents call to WaitOne if task has already completed
            return Yield<T>(
                async (yieldReturn, yieldBreak) =>
                {
                    bool Complete()
                    {
                        if (taskComplete)
                            return true;

                        var taskIsFinished = complete.WaitOne(0);
                        if (taskIsFinished)
                        {
                            taskComplete = true; // prevent hanging on complete.WaitOne
                            return true;
                        }

                        return false;
                    }

                    while (!Complete())
                    {
                        moved.WaitOne(TimeSpan.FromSeconds(5));

                        var yieldKvp = await YieldResultAsync();
                        if (!yieldKvp.Key)
                            continue;
                        return yieldKvp.Value;
                    }

                    var yieldFinalKvp = await YieldResultAsync();
                    if (!yieldFinalKvp.Key)
                        return yieldBreak;
                    return yieldFinalKvp.Value;

                    async Task<KeyValuePair<bool, IYieldResult<T>>> YieldResultAsync()
                    {
                        Task<T>[] nextSegment;
                        lock (segment)
                        {
                            nextSegment = segment.ToArray();
                        }
                        if (nextSegment.Any())
                        {
                            var finishedTaskNext = maintainOrder ?
                                nextSegment.First()
                                :
                                await Task.WhenAny<T>(nextSegment);
                            lock (segment)
                            {
                                segment.Remove(finishedTaskNext);
                            }
                            var next = await finishedTaskNext;
                            return yieldReturn(next).PairWithKey(true);
                        }
                        return default(IYieldResult<T>).PairWithKey(false);
                    }
                });
        }

        public static IEnumerableAsync<T> Prespool<T>(this IEnumerableAsync<T> items, ILogger diagnosticsTag = default(ILogger))
        {
            // TODO: Match batch use this instead of this using Batch.
            return items.Batch(diagnosticsTag).SelectMany();
        }

        public static IEnumerableAsync<TResult> Range<TItem, TRange, TResult>(this IEnumerableAsync<TItem> enumerables,
            Func<TItem, TRange> min,
            Func<TItem, TRange> max,
            Func<TRange, TRange, IEnumerable<TResult>> range)
        {

            throw new NotImplementedException();

            //var enumerators = enumerables
            //    .Select((enumerable, index) => index.PairWithValue(enumerable.GetEnumerator()))
            //    .ToDictionary();

            //var tasks = enumerators
            //    .Select(enumerator => enumerator.Key.PairWithValue(enumerator.Value.MoveNextAsync()))
            //    .ToArray();

            //return Yield<T>(
            //    async (yieldReturn, yieldBreak) =>
            //    {
            //        while (true)
            //        {
            //            var finishedTaskKvp = await GetCompletedTaskIndex<bool>(tasks);
            //            var finishedTaskIndex = finishedTaskKvp.Key;
            //            var moved = finishedTaskKvp.Value;
            //            var enumerator = enumerators[finishedTaskIndex];
            //            tasks = tasks.Where(task => task.Key != finishedTaskIndex).ToArray();
            //            if (moved)
            //            {
            //                var current = enumerator.Current;
            //                tasks = tasks.Append(finishedTaskIndex.PairWithValue(enumerators[finishedTaskIndex].MoveNextAsync())).ToArray();
            //                return yieldReturn(current);
            //            }
            //            if (!tasks.Any())
            //                return yieldBreak;
            //        }
            //    });
        }
        
        public static IEnumerableAsync<TItem> Compress<TItem>(this IEnumerableAsync<TItem> enumerables,
            Func<TItem, TItem, IEnumerableAsync<TItem>> compressor)
        {
            throw new NotImplementedException();

            //var enumerators = enumerables
            //    .Select((enumerable, index) => index.PairWithValue(enumerable.GetEnumerator()))
            //    .ToDictionary();

            //var tasks = enumerators
            //    .Select(enumerator => enumerator.Key.PairWithValue(enumerator.Value.MoveNextAsync()))
            //    .ToArray();

            //return Yield<T>(
            //    async (yieldReturn, yieldBreak) =>
            //    {
            //        while (true)
            //        {
            //            var finishedTaskKvp = await GetCompletedTaskIndex<bool>(tasks);
            //            var finishedTaskIndex = finishedTaskKvp.Key;
            //            var moved = finishedTaskKvp.Value;
            //            var enumerator = enumerators[finishedTaskIndex];
            //            tasks = tasks.Where(task => task.Key != finishedTaskIndex).ToArray();
            //            if (moved)
            //            {
            //                var current = enumerator.Current;
            //                tasks = tasks.Append(finishedTaskIndex.PairWithValue(enumerators[finishedTaskIndex].MoveNextAsync())).ToArray();
            //                return yieldReturn(current);
            //            }
            //            if (!tasks.Any())
            //                return yieldBreak;
            //        }
            //    });
        }

        public static IEnumerableAsync<T> SelectMany<T>(this IEnumerable<IEnumerableAsync<T>> enumerables,
            bool sequential = false)
        {
            if (sequential)
                return SelectManySequential(enumerables);
            return SelectManyNonSequential(enumerables);
        }

        private static IEnumerableAsync<T> SelectManySequential<T>(IEnumerable<IEnumerableAsync<T>> enumerables)
        {
            var enumerator = enumerables.GetEnumerator();

            IEnumeratorAsync<T> enumeratorInner;
            while (true)
            {
                if (!enumerator.MoveNext())
                    return EnumerableAsync.Empty<T>();
                // Catch unlikely case where returned EnumerableAsync is null or default;
                if (enumerator.Current.IsDefaultOrNull())
                    continue;

                enumeratorInner = enumerator.Current.GetEnumerator();
                break;
            }

            return Yield<T>(
                async (yieldReturn, yieldBreak) =>
                {
                    while (true)
                    {
                        if (await enumeratorInner.MoveNextAsync())
                            return yieldReturn(enumeratorInner.Current);

                        while (true)
                        {
                            if (!enumerator.MoveNext())
                                return yieldBreak;

                            // Catch unlikely case where returned EnumerableAsync is null or default;
                            if (enumerator.Current.IsDefaultOrNull())
                                continue;

                            enumeratorInner = enumerator.Current.GetEnumerator();
                            break;
                        }
                    }
                });
        }

        private static IEnumerableAsync<T> SelectManyNonSequential<T>(IEnumerable<IEnumerableAsync<T>> enumerables)
        {
            var allTask = new List<Task<KeyValuePair<bool, T>>>();
            var allTaskLock = new object();
            var taskGenerator = enumerables
                .Select(
                    enumerable =>
                    {
                        var enumerator = enumerable.GetEnumerator();
                        async Task<KeyValuePair<bool, T>> NextTask()
                        {
                            var moved = await enumerator.MoveNextAsync();
                            if (!moved)
                                return default(T).PairWithKey(false);

                            lock (allTaskLock)
                            {
                                allTask.Add(NextTask());
                            }
                            var current = enumerator.Current;
                            return current.PairWithKey(true);
                        }
                        lock (allTaskLock)
                        {
                            allTask.Add(NextTask());
                        }
                        return enumerator;
                    })
                 .GetEnumerator();

            var generating = true;
            return Yield<T>(
                async (yieldReturn, yieldBreak) =>
                {
                    async Task<KeyValuePair<bool, T>> YieldValueAsync(Task<KeyValuePair<bool, T>> task)
                    {
                        var result = await task;
                        lock (allTaskLock)
                        {
                            allTask.Remove(task);
                        }
                        return result;
                    }

                    while (true)
                    {
                        Task<KeyValuePair<bool, T>>[] tasks;
                        lock (allTaskLock)
                        {
                            tasks = allTask.ToArray();
                        }
                        
                        foreach (var task in tasks)
                        {
                            if (task.Status != TaskStatus.RanToCompletion)
                                continue;

                            var result = await YieldValueAsync(task);
                            if (result.Key)
                                return yieldReturn(result.Value);
                        }

                        if (generating)
                        {
                            if (!taskGenerator.MoveNext())
                                generating = false;
                            continue;
                        }

                        if (!tasks.Any())
                            return yieldBreak;

                        var nextTask = tasks.GetFirstSuccessfulTask();
                        var next = await YieldValueAsync(nextTask);
                        if (next.Key)
                            return yieldReturn(next.Value);
                    }
                });
        }

        public static IEnumerableAsync<TItem> SelectMany<TItem>(this IEnumerableAsync<IEnumerable<TItem>> enumerables)
        {
            return enumerables.SelectMany<IEnumerable<TItem>, TItem>(x => x);
        }
        
        public static IEnumerableAsync<TResult> SelectMany<T, TResult>(this IEnumerableAsync<T> enumerables, Func<T, IEnumerable<TResult>> selectMany)
        {
            var enumerator = enumerables.GetEnumerator();
            var enumeratorInner = default(IEnumerator<TResult>);
            return Yield<TResult>(
                async (yieldReturn, yieldBreak) =>
                {
                    while (true)
                    {
                        if (enumeratorInner.IsDefaultOrNull())
                        {
                            if (!await enumerator.MoveNextAsync())
                                return yieldBreak;
                            var current = enumerator.Current;
                            var many = selectMany(current);
                            if (many.IsDefaultOrNull())
                                continue;
                            enumeratorInner = many.GetEnumerator();
                            continue;
                        }
                        if (!enumeratorInner.MoveNext())
                        {
                            if (!await enumerator.MoveNextAsync())
                                return yieldBreak;
                            enumeratorInner = selectMany(enumerator.Current).GetEnumerator();
                            continue;
                        }
                        return yieldReturn(enumeratorInner.Current);
                    }
                });
        }
        
        public static IEnumerableAsync<TResult> SelectAsyncMany<T, TResult>(this IEnumerableAsync<T> enumerables, Func<T, IEnumerableAsync<TResult>> selectMany)
        {
            var enumerator = enumerables.GetEnumerator();
            var enumeratorInner = default(IEnumeratorAsync<TResult>);
            return Yield<TResult>(
                async (yieldReturn, yieldBreak) =>
                {
                    while (true)
                    {
                        if (enumeratorInner.IsDefaultOrNull())
                        {
                            if (!await enumerator.MoveNextAsync())
                                return yieldBreak;
                            enumeratorInner = selectMany(enumerator.Current).GetEnumerator();
                            continue;
                        }

                        if (!await enumeratorInner.MoveNextAsync())
                        {
                            if (!await enumerator.MoveNextAsync())
                                return yieldBreak;
                            enumeratorInner = selectMany(enumerator.Current).GetEnumerator();
                            continue;
                        }

                        return yieldReturn(enumeratorInner.Current);
                    }
                });
        }
        
        public static IEnumerableAsync<T> SelectAsyncMany<T>(this IEnumerableAsync<IEnumerableAsync<T>> enumerables)
        {
            return enumerables.SelectAsyncMany(items => items);
        }
        
        public static IEnumerableAsync<T> Concat<T>(this IEnumerableAsync<T> enumerable1, IEnumerableAsync<T> enumerable2)
        {
            return (new IEnumerableAsync<T> []
            {
                enumerable1,
                enumerable2,
            })
            .AsyncConcat();
        }

        public static IEnumerableAsync<T> Concat<T>(this IEnumerableAsync<T> enumerable1, IEnumerable<T> enumerable2)
        {
            var enumerable2Async = enumerable2.Select(item => item.AsTask()).AsyncEnumerable();
            return enumerable1.Concat(enumerable2Async);
        }

        public static IEnumerableAsync<T> AsyncConcat<T>(this IEnumerable<IEnumerableAsync<T>> enumerables)
        {
            var enumerator = enumerables.GetEnumerator();
            var enumeratorInner = default(IEnumeratorAsync<T>);
            return Yield<T>(
                async (yieldReturn, yieldBreak) =>
                {
                    while (true)
                    {
                        if (enumeratorInner.IsDefaultOrNull() || (!await enumeratorInner.MoveNextAsync()))
                        {
                            if (!enumerator.MoveNext())
                                return yieldBreak;

                            enumeratorInner = enumerator.Current.GetEnumerator();
                            if (!await enumeratorInner.MoveNextAsync())
                                continue;
                        }
                        return yieldReturn(enumeratorInner.Current);
                    }
                });
        }
        
        public static IEnumerableAsync<T> Append<T>(this IEnumerableAsync<T> enumerable1, T enumerable2)
        {
            return (new IEnumerableAsync<T>[]
            {
                enumerable1,
                enumerable2.EnumerableAsyncStart(),
            })
            .AsyncConcat();
        }

        public static IEnumerableAsync<T> AsyncAppend<T>(this IEnumerableAsync<T> enumerable1, Task<T> enumerable2)
        {
            return (new IEnumerableAsync<T>[]
            {
                enumerable1,
                enumerable2.AsEnumerable(),
            })
            .AsyncConcat();
        }

        public static IEnumerableAsync<T> AppendOptional<T>(this IEnumerableAsync<T> enumerableAsync,
            Func<Func<T, IEnumerableAsync<T>>, Func<IEnumerableAsync<T>>, IEnumerableAsync<T>> appendOptional)
        {
            return appendOptional(
                (item) => enumerableAsync.Append(item),
                () => enumerableAsync);
        }

        public static IEnumerableAsync<T> AppendAsyncOptional<T>(this IEnumerableAsync<T> enumerableAsync,
            Func<Func<T, IEnumerableAsync<T>>, Func<IEnumerableAsync<T>>, Task<IEnumerableAsync<T>>> appendOptionalAsync)
        {
            return appendOptionalAsync(
                (item) => enumerableAsync.Append(item),
                () => enumerableAsync)
                .FoldTask();
        }

        public static IEnumerableAsync<TItem> ConcatIfAny<TItem>(this IEnumerableAsync<TItem> enumerable1,
            Func<IEnumerableAsync<TItem>> onSome)
        {
            bool hasExecuted = false;
            var iterator = default(IEnumeratorAsync<TItem>);
            var enumerator1 = enumerable1.GetEnumerator();
            bool enumerator1Terminated = false;
            bool wasAny = false;
            return EnumerableAsync.Yield<TItem>(
                async (yieldContinue, yieldBreak) =>
                {
                    if (!enumerator1Terminated)
                    {
                        var next = await enumerator1.MoveNextAsync();
                        if (next)
                        {
                            wasAny = true;
                            return yieldContinue(enumerator1.Current);
                        }
                        enumerator1Terminated = true;
                    }

                    if (!wasAny)
                        return yieldBreak;

                    if (!hasExecuted)
                    {
                        var some = onSome();
                        iterator = some.GetEnumerator();
                        hasExecuted = true;
                    }
                    if (await iterator.MoveNextAsync())
                        return yieldContinue(iterator.Current);
                    return yieldBreak;
                });
        }


        public static Task<KeyValuePair<int, T>> GetCompletedTaskIndex<T>(this IEnumerable<KeyValuePair<int, Task<T>>> tasks)
        {
            var tcs = new TaskCompletionSource<KeyValuePair<int, T>>();
            int remainingTasks = tasks.Count();
            foreach (var task in tasks)
            {
                task.Value.ContinueWith(t =>
                {
                    if (task.Value.Status == TaskStatus.RanToCompletion)
                        tcs.TrySetResult(task.Key.PairWithValue(task.Value.Result));
                    else if (System.Threading.Interlocked.Decrement(ref remainingTasks) == 0)
                        tcs.SetException(new AggregateException(
                            tasks.SelectMany(t2 => t2.Value.Exception?.InnerExceptions ?? Enumerable.Empty<Exception>())));
                });
            }
            return tcs.Task;
        }

        public static Task<T> GetFirstSuccessfulTask<T>(this IEnumerable<Task<T>> tasks)
        {
            var tcs = new TaskCompletionSource<T>();
            int remainingTasks = tasks.Count();
            foreach (var task in tasks)
            {
                task.ContinueWith(
                    t =>
                    {
                        if (task.Status == TaskStatus.RanToCompletion)
                            tcs.TrySetResult(t.Result);
                        else if (System.Threading.Interlocked.Decrement(ref remainingTasks) == 0)
                            tcs.SetException(new AggregateException(
                                tasks.SelectMany(t2 => t2.Exception?.InnerExceptions ?? Enumerable.Empty<Exception>())));
                    });
            }
            return tcs.Task;
        }



        public static IEnumerableAsync<TResult> Zip<T1, T2, TResult>(this IEnumerableAsync<T1> items1, 
            IEnumerableAsync<T2> items2, Func<T1, T2, TResult> zipper)
        {
            var enumerator1 = items1.GetEnumerator();
            var enumerator2 = items2.GetEnumerator();
            return Yield<TResult>(
                async (yieldReturn, yieldBreak) =>
                {
                    var moved1Task = enumerator1.MoveNextAsync();
                    var moved2Task = enumerator2.MoveNextAsync();
                    var moved1 = await moved1Task;
                    if(!moved1)
                        return yieldBreak;

                    var moved2 = await moved2Task;
                    if (!moved2)
                        return yieldBreak;

                    var value1 = enumerator1.Current;
                    var value2 = enumerator2.Current;
                    var zipped = zipper(value1, value2);
                    return yieldReturn(zipped);
                });
        }

        public static IEnumerableAsync<T> FoldTask<T>(this Task<IEnumerableAsync<T>> enumerable, string tag = default(string))
        {
            var enumerator = default(IEnumeratorAsync<T>);
            return Yield<T>(
                async (yieldReturn, yieldBreak) =>
                {
                    if(enumerator.IsDefaultOrNull())
                        enumerator = (await enumerable).GetEnumerator();
                    
                    if (!await enumerator.MoveNextAsync())
                        return yieldBreak;

                    var next = enumerator.Current;
                    return yieldReturn(next);
                });
        }
        
        public static IEnumerableAsync<T> Await<T>(this IEnumerableAsync<Task<T>> enumerable, string tag = default(string))
        {
            var enumerator = enumerable.GetEnumerator();
            return Yield<T>(
                async (yieldReturn, yieldBreak) =>
                {
                    if (!tag.IsNullOrWhiteSpace())
                        Console.WriteLine($"Await[{tag}]:Requesting next");

                    if (!await enumerator.MoveNextAsync())
                    {
                        if (!tag.IsNullOrWhiteSpace())
                            Console.WriteLine($"Await[{tag}]:Completed");
                        return yieldBreak;
                    }

                    if (!tag.IsNullOrWhiteSpace())
                        Console.WriteLine($"Await[{tag}]:Awaiting value next");
                    var next = await enumerator.Current;
                    
                    if (!tag.IsNullOrWhiteSpace())
                        Console.WriteLine($"Await[{tag}]:Yielding value");
                    return yieldReturn(next);
                });
        }

        [Obsolete("Use AggregateAsync")]
        public static async Task<TResult[]> AsyncAggregateAsync<TItem, TResult>(this IEnumerableAsync<TItem> enumerable,
            Func<TResult[], TItem, Task<TResult[]>> funcAsync)
        {
            var accumulation = new TResult[] { }; // TODO: Should be a hash
            var enumeratorAsync = enumerable.GetEnumerator();
            while(await enumeratorAsync.MoveNextAsync())
            {
                var current = enumeratorAsync.Current;
                accumulation = await funcAsync(accumulation, current);
            }
            return accumulation;
        }

    }
}

using BlackBarLabs.Extensions;
using EastFive.Analytics;
using EastFive.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EastFive.Linq.Async
{
    public interface IYieldResult<T>
    {
        T Value { get; }

        Task<bool> HasNext(
            Func<IYieldResult<T>, bool> onMore,
            Func<Exception, bool> onException,
            Func<bool> onEnd);
    }

    public static partial class EnumerableAsync
    {
        public delegate Task<IYieldResult<T>> YieldDelegateAsync<T>(
                Func<T, IYieldResult<T>> yieldReturn,
                IYieldResult<T> yieldBreak);

        private struct YieldEnumerable<T> : IEnumerableAsync<T>
        {
            private class YieldResult : IYieldResult<T>
            {
                private T value;
                private YieldDelegateAsync<T> getNext;
                private Task<IYieldResult<T>> fetch;
                private bool hasFeched;

                internal YieldResult(T value, YieldDelegateAsync<T> next)
                {
                    this.value = value;
                    this.getNext = next;
                    this.hasFeched = false;
                }

                public virtual T Value => value;

                public async Task<bool> HasNext(
                    Func<IYieldResult<T>, bool> onMore,
                    Func<Exception, bool> onException,
                    Func<bool> onEnd)
                {
                    Task<IYieldResult<T>> internalFetch;
                    lock(this)
                    {
                        if (!this.hasFeched)
                        {
                            this.fetch = getNext(
                                v =>
                                {
                                    return new YieldResult(v, this.getNext);
                                },
                                new YieldBreak());
                            this.hasFeched = true;
                        }
                        internalFetch = this.fetch;
                    }

                    try
                    {
                        var yieldResult = await internalFetch;
                        var isTerminal = (yieldResult is YieldBreak);
                        if (isTerminal)
                            return onEnd();
                        return onMore(yieldResult);
                    }catch (Exception ex)
                    {
                        ex.GetType();// suppress warning
                        throw;
                    }
                }

                private struct YieldBreak : IYieldResult<T>
                {
                    public T Value => throw new NullReferenceException();

                    public Task<bool> HasNext(
                        Func<IYieldResult<T>, bool> onMore,
                        Func<Exception, bool> onException,
                        Func<bool> onEnd)
                    {
                        return onEnd().ToTask();
                    }
                }

            }

            private class YieldResultFirst : YieldResult
            {
                internal YieldResultFirst(YieldDelegateAsync<T> next)
                    : base(default(T), next)
                {
                }

                public override T Value => throw new NullReferenceException();
                
            }

            // private YieldDelegateAsync<T> yield;
            private IYieldResult<T> firstStep;

            internal YieldEnumerable(YieldDelegateAsync<T> yield)
            {
                this.firstStep = new YieldResultFirst(yield);
            }
            
            private class YieldEnumerator : IEnumeratorAsync<T>
            {
                private IYieldResult<T> currentStep;
                public YieldEnumerator(IYieldResult<T> firstStep)
                {
                    this.currentStep = firstStep;
                }

                public T Current => currentStep.Value;

                public Task<bool> MoveNextAsync()
                {
                    return this.currentStep.HasNext(
                        (next) =>
                        {
                            this.currentStep = next;
                            return true;
                        },
                        (ex) =>
                        {
                            return true;
                        },
                        () =>
                        {
                            //this.currentStep = null;
                            return false;
                        });
                }
            }

            public IEnumeratorAsync<T> GetEnumerator()
            {
                return new YieldEnumerator(firstStep);
            }
        }

        public static IEnumerableAsync<T> Yield<T>(
            YieldDelegateAsync<T> generateFunction)
        {
            return new YieldEnumerable<T>(generateFunction);
        }

        public static IEnumerableAsync<T> YieldBatch<T>(
            YieldDelegateAsync<T[]> generateFunction,
            CancellationToken cancellationToken = default)
        {
            return Yield<T[]>(generateFunction)
                .SelectMany(cancellationToken:cancellationToken);
        }

        public static IEnumerableAsync<T> Range<T>(int start, int count,
            Func<int, Task<T>> generateFunction)
        {
            return Yield<T>(
                async (yieldReturn, yieldBreak) =>
                {
                    if (count == 0)
                        return yieldBreak;
                    count--;
                    var index = start;
                    start++;
                    var value = await generateFunction(index);
                    return yieldReturn(value);
                });
        }

        public static IEnumerableAsync<T> From<T>(params T[] items)
        {
            var count = -1;
            return Yield<T>(
                async (yieldReturn, yieldBreak) =>
                {
                    count++;
                    if (items.Length == count)
                        return await yieldBreak.AsTask();

                    var item = items[count];
                    return yieldReturn(item);
                });
        }

        public static IEnumerableAsync<TItem> AsyncEnumerable<TItem>(this IEnumerable<Task<TItem>> items)
        {
            var enumerator = items.GetEnumerator();
            return Yield<TItem>(
                async (yieldReturn, yieldBreak) =>
                {
                    if (!enumerator.MoveNext())
                        return yieldBreak;

                    var current = await enumerator.Current;
                    return yieldReturn(current);
                });
        }

        public static IEnumerableAsync<TItem> AsyncEnumerable<TItem>(this IEnumerable<Task<TItem>> items,
            int readAhead = 1)
        {
            // No arg exception here because some items want to read ahead 
            // using a dynamic value which could be 0 and ...
            if (readAhead < 1)
                // ... the readAhead >= 1 requirement is based off the implementation of this method.
                readAhead = 1; 

            var enumerator = items.GetEnumerator();
            bool moreDataToRead = enumerator.MoveNext();

            //var batchQueue = new Queue<Task<TItem>>(readAhead);
            var batchQueue = new System.Collections.Concurrent.ConcurrentQueue<Task<TItem>>();
            var batchQueueLock = new object();

            bool ShouldQueueMoreItems()
            {
                if (!moreDataToRead)
                    return false;

                if (batchQueue.Count < readAhead)
                    return true;

                return false;
            }

            while (ShouldQueueMoreItems())
            {
                batchQueue.Enqueue(enumerator.Current);
                moreDataToRead = enumerator.MoveNext();
            }

            return Yield<TItem>(
                async (yieldReturn, yieldBreak) =>
                {
                    Task<TItem> currentTask;

                    lock (batchQueueLock)
                    {
                        while (true)
                        {
                            while (ShouldQueueMoreItems())
                            {
                                batchQueue.Enqueue(enumerator.Current);
                                moreDataToRead = enumerator.MoveNext();
                            }

                            //if (batchQueue.Any())
                            if (batchQueue.TryDequeue(out currentTask))
                                break;

                            if (!moreDataToRead)
                                return yieldBreak;
                        }
                        //currentTask = batchQueue.Dequeue();
                    }

                    var current = await currentTask;
                    return yieldReturn(current);
                });
        }

        public static IEnumerableAsync<TItem> AsyncEnumerable<TItem>(this IEnumerable<Task<TItem>> items,
            bool startAllTasks)
        {
            var tasks = items.ToArray();
            var batchQueue = new System.Collections.Concurrent.ConcurrentQueue<Task<TItem>>(tasks);
            
            return Yield<TItem>(
                async (yieldReturn, yieldBreak) =>
                {
                    if (!batchQueue.TryDequeue(out Task<TItem>  currentTask))
                        return yieldBreak;

                    var current = await currentTask;
                    return yieldReturn(current);
                });
        }

        public static IEnumerableAsync<TItem> AsAsync<TItem>(this IEnumerable<TItem> items)
        {
            var enumerator = items.GetEnumerator();
            return Yield<TItem>(
                async (yieldReturn, yieldBreak) =>
                {
                    if (!enumerator.MoveNext())
                        return await yieldBreak.AsTask();

                    var current = enumerator.Current;
                    return yieldReturn(current);
                });
        }

        public static IEnumerableAsync<TItem> FoldTask<TItem>(this Task<IEnumerable<TItem>> itemsTask)
        {
            var enumerator = default(IEnumerator<TItem>);
            return Yield<TItem>(
                async (yieldReturn, yieldBreak) =>
                {
                    if (enumerator.IsDefault())
                    {
                        var items = await itemsTask;
                        enumerator = items.GetEnumerator();
                    }
                    if (!enumerator.MoveNext())
                        return yieldBreak;

                    var current = enumerator.Current;
                    return yieldReturn(current);
                });
        }

        public static IEnumerableAsync<TItem> EnumerableAsyncStart<TItem>(this TItem item)
        {
            bool yielded = false;
            return Yield<TItem>(
                (yieldReturn, yieldBreak) =>
                {
                    if (!yielded)
                    {
                        yielded = true;
                        return yieldReturn(item).AsTask();
                    }
                    return yieldBreak.AsTask();
                });
        }

        public static IEnumerableAsync<TItem> AsEnumerable<TItem>(this Task<TItem> itemTask)
        {
            bool yielded = false;
            return Yield<TItem>(
                async (yieldReturn, yieldBreak) =>
                {
                    if (!yielded)
                    {
                        yielded = true;
                        var item = await itemTask;
                        return yieldReturn(item);
                    }
                    return yieldBreak;
                });
        }

        public static IEnumerableAsync<T> Parallel<T>(this IEnumerable<Task<T>> enumerable,
            bool maintainOrder = false,
            ILogger diagnostics = default(ILogger))
        {
            var segment = new List<Task<T>>();
            var moved = new AutoResetEvent(false);
            var complete = new ManualResetEvent(false);
            var segmentTask = Task.Run(
                () =>
                {
                    var enumerator = enumerable.GetEnumerator();
                    while(enumerator.MoveNext())
                    {
                        lock(segment)
                        {
                            segment.Add(enumerator.Current);
                        }
                        moved.Set();
                    }
                    complete.Set();
                });
            var taskComplete = false; // Prevents call to WaitOne if task has already completed
            return Yield<T>(
                async (yieldReturn, yieldBreak) =>
                {
                    async Task<bool> CompleteAsync()
                    {
                        if (taskComplete)
                            return true;

                        var taskIsFinished = complete.WaitOne(0);
                        if (!taskIsFinished)
                            return false;

                        taskComplete = true; // prevent hanging on complete.WaitOne(0)
                        await segmentTask; // wrap up batch read-ahead task
                        return true;
                    }

                    while (!await CompleteAsync())
                    {
                        moved.WaitOne(TimeSpan.FromSeconds(5));

                        var yieldKvp = await YieldResultAsync();
                        if (!yieldKvp.Key)
                            continue;
                        return yieldKvp.Value;
                    }

                    var yieldFinalKvp = await YieldResultAsync();
                    if (!yieldFinalKvp.Key)
                    {
                        return yieldBreak;
                    }
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

        public static IEnumerableAsync<TItem> Throttle<TItem>(this IEnumerable<Task<TItem>> enumerable,
            int desiredRunCount = 1,
            ILogger log = default(ILogger))
        {
            var logScope = log.CreateScope($"Throttle");
            var taskList = new List<Task<TItem>>();
            var enumerator = enumerable.GetEnumerator();
            var moving = true;
            return Yield<TItem>(
                async (yieldReturn, yieldBreak) =>
                {
                    while (true)
                    {
                        bool DequeueAnotherTask()
                        {
                            if (!moving)
                                return false;

                            lock (taskList)
                            {
                                if (taskList.Count >= desiredRunCount)
                                    return false;
                            }

                            return true;
                        }

                        if(DequeueAnotherTask())
                        {
                            if (!enumerator.MoveNext())
                            {
                                moving = false;
                                continue;
                            }
                            var nextTask = enumerator.Current;
                            lock (taskList)
                            {
                                taskList.Add(nextTask);
                            }
                            continue;
                        }

                        var finishedTaskTask = default(Task<Task<TItem>>);
                        lock (taskList)
                        {
                            if (!taskList.Any())
                                if (!moving)
                                    return yieldBreak;

                            Task<TItem>[] tasks = taskList.ToArray();
                            finishedTaskTask = Task.WhenAny(tasks);
                        }
                        if (finishedTaskTask.IsDefaultOrNull())
                            continue;

                        var finishedTask = await finishedTaskTask;
                        lock (taskList)
                        {
                            taskList.Remove(finishedTask);
                        }
                        var next = await finishedTask;
                        return yieldReturn(next);
                    }
                });
        }

        public static IEnumerableAsync<TResult> SelectAsyncWith<TWith, TItem, TResult>(this IEnumerable<TItem> items,
            TWith seed, Func<TWith, TItem, Task<(TResult, TWith)>> callback)
        {
            var carry = seed;
            var enumerator = items.GetEnumerator();
            return Yield<TResult>(
                async (yieldReturn, yieldBreak) =>
                {
                    if (!enumerator.MoveNext())
                        return yieldBreak;
                    var (next, newCarry) = await callback(carry, enumerator.Current);
                    return yieldReturn(next);
                });
        }

        #region SelectWhere Tuples

        public static IEnumerableAsync<T3> SelectWhere<T1, T2, T3>(this IEnumerableAsync<(T1, T2)> items,
            Func<(T1, T2), (bool, T3)> isWhere)
        {
            var enumerator = items.GetEnumerator();
            return EnumerableAsync.Yield<T3>(
                async (yieldReturn, yieldBreak) =>
                {
                    while (true)
                    {
                        if (!await enumerator.MoveNextAsync())
                            return yieldBreak;

                        var current = enumerator.Current;
                        var (isSelected, r) = isWhere(current);
                        if (isSelected)
                            return yieldReturn(r);
                    }
                });
        }

        public static IEnumerableAsync<T> SelectWhere<T>(this IEnumerableAsync<(bool, T)> items)
        {
            return items.SelectWhere(
                item => (item.Item1, item.Item2));
        }

        public static IEnumerableAsync<(T4, T5)> SelectWhere<T1, T2, T3, T4, T5>(this IEnumerableAsync<(T1, T2, T3)> items,
            Func<(T1, T2, T3), (bool, T4, T5)> isWhere)
        {
            return items
                .Select(item => isWhere(item))
                .Select(tpl => (tpl.Item1, (tpl.Item2, tpl.Item3)))
                .SelectWhere();
        }

        public static IEnumerableAsync<(T2, T3)> SelectWhere<T2, T3>(this IEnumerableAsync<(bool, T2, T3)> items)
        {
            return items.SelectWhere(
                item => (item.Item1, item.Item2, item.Item3));
        }

        #endregion

        public interface ISelected<T>
        {
            bool HasValue { get; }
            T Value { get; }
        }

        private struct SelectedValue<T> : ISelected<T>
        {
            public SelectedValue(bool x)
            {
                this.HasValue = false;
                this.Value = default(T);
            }

            public SelectedValue(T nextItem)
            {
                this.HasValue = true;
                this.Value = nextItem;
            }

            public bool HasValue { get; private set; }

            public T Value { get; private set; }
        }
        
    }
}

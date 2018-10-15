using EastFive.Extensions;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EastFive.Collections;
using EastFive.Collections.Generic;
using EastFive.Linq;
using BlackBarLabs;

namespace EastFive.Linq.Async
{
    public interface IManagePerformance<TResult>
    {
        Task<TResult> Manage(Func<Task<TResult>> criticalSection);
    }
    
    public static partial class EnumerableAsync
    {
        private class PerformanceManager<TResult> : IManagePerformance<TResult>
        {
            private List<Timing> timings = new List<Timing>();
            private Queue<ManualResetEvent> workQueue = new Queue<ManualResetEvent>();
            private List<ManualResetEvent> concurrentThreads = new List<ManualResetEvent>();
            private DateTime epoch = DateTime.UtcNow;
            private double predictor = 0.0;
            private Random rand = new Random();

            public async Task<TResult> Manage(Func<Task<TResult>> criticalSection)
            {
                ManualResetEvent mutex;
                lock (timings)
                {
                    var concurrencyEmpty = !concurrentThreads.Any();
                    mutex = new ManualResetEvent(concurrencyEmpty);
                    workQueue.Enqueue(mutex);
                }

                mutex.WaitOne();

                lock (timings)
                {
                    concurrentThreads.Add(mutex);
                }

                var when = (DateTime.UtcNow - epoch);
                var stopWatch = new System.Diagnostics.Stopwatch();
                stopWatch.Start();

                var task = criticalSection();

                await task.ContinueWith(
                    (taskCompleted) =>
                    {
                        stopWatch.Stop();
                        lock(timings)
                        {
                            var concurrentEnd = concurrentThreads.Count;
                            concurrentThreads.Remove(mutex);
                            var durationTicks = stopWatch.Elapsed.Ticks / concurrentEnd;
                            timings.Add(
                                new Timing
                                {
                                    duration = TimeSpan.FromTicks(durationTicks),
                                    when = when,
                                });


                            var averageDuration = timings.Average(timing => timing.duration.Ticks);
                            var predictorChange = (durationTicks < averageDuration) ?
                                (1.0 - predictor) / 2.0
                                :
                                (-1.0 - predictor) / 2.0;
                            var predictorChangeWeighted = predictorChange / ((double)timings.Count);
                            predictor = predictorChangeWeighted;

                            // Ensure this ContinueWith block gets called again
                            if (!concurrentThreads.Any())
                            {
                                if (!workQueue.Any())
                                    return;
                                var nextEntry = workQueue.Dequeue();
                                nextEntry.Set();
                            }

                            // Launch more tasks
                            while(true)
                            {
                                if (!workQueue.Any())
                                    break;

                                var randomValueZeroCentered = (rand.NextDouble() * 2.0) - 1.0;
                                if (randomValueZeroCentered > predictor)
                                    break;

                                var nextEntry = workQueue.Dequeue();
                                nextEntry.Set();
                            }
                        }
                    }, TaskContinuationOptions.ExecuteSynchronously);
                return await task;
            }
        }

        private class UnthrottledPerformanceManager<TResult> : IManagePerformance<TResult>
        {
            public Task<TResult> Manage(Func<Task<TResult>> criticalSection)
            {
                return criticalSection();
            }
        }

        public static IManagePerformance<T> GetDefaultIfNull<T>(this IManagePerformance<T> performanceManagerMaybe)
        {
            if (performanceManagerMaybe.IsDefaultOrNull())
                return new UnthrottledPerformanceManager<T>();

            return performanceManagerMaybe;
        }

        private class CastedPerformanceManager<TSource, TConverted> : IManagePerformance<TConverted>
        {
            private IManagePerformance<TSource> sourceManager;
            private Func<TConverted, TSource> cast;

            public CastedPerformanceManager(IManagePerformance<TSource> sourceManager, Func<TConverted, TSource> cast)
            {
                this.sourceManager = sourceManager;
                this.cast = cast;
            }

            public async Task<TConverted> Manage(Func<Task<TConverted>> criticalSection)
            {
                var storage = default(TConverted);
                var result2 = await sourceManager.Manage(
                    async () =>
                    {
                        storage = await criticalSection();
                        var r1 = cast(storage);
                        return r1;
                    });
                return storage;
            }
        }

        public static IManagePerformance<TConverted> CastManager<TSource, TConverted>(this IManagePerformance<TSource> performanceManager1, Func<TConverted, TSource> cast)
        {
            return new CastedPerformanceManager<TSource, TConverted>(performanceManager1, cast);
        }

        private struct Timing
        {
            public TimeSpan when;
            public TimeSpan duration;
        }

        private class QueueEntry<T>
        {
            public T value;
            public ManualResetEvent hasValue;
        }

        private class ThrottleScheduler<T> : TaskScheduler
        {
            private Dictionary<int, List<Timing>> timings;
            private List<Task<T>> tasks;
            Random rand = new Random();
            
            private int delegatesQueuedOrRunning;
            [ThreadStatic]
            private static bool _currentThreadIsProcessingItems;
            private readonly LinkedList<Task> _tasks = new LinkedList<Task>(); // protected by lock(_tasks)

            

            public ThrottleScheduler(int v)
            {
                this.timings = new Dictionary<int, List<Timing>>();
                this.tasks = new List<Task<T>>();
            }

            protected int MaxDegreeOfParallelism
            {
                get
                {
                    lock (timings)
                    {
                        // TODO: Include confidence values
                        var performanceNumbers = timings
                            .Select(
                                kvp => kvp.Value.Average(t => t.duration.TotalMilliseconds).PairWithKey(kvp.Key))
                            .OrderBy(kvp => kvp.Value);

                        if (!performanceNumbers.Any())
                            return 1;

                        var max = performanceNumbers
                            .First()
                            .Key;

                        // TODO: Stochastic method paired with confidence values here
                        if (rand.Next() % 22 == 0)
                            return max + 1;

                        return Math.Max(1, max);
                    }
                }
            }

            protected int ActiveParallelism
            {
                get
                {
                    return this.tasks
                        .Where(task =>
                            task.Status == TaskStatus.Created ||
                            task.Status == TaskStatus.Running ||
                            task.Status == TaskStatus.WaitingForActivation ||
                            task.Status == TaskStatus.WaitingForChildrenToComplete ||
                            task.Status == TaskStatus.WaitingToRun)
                        .Count();
                }
            }

            public void AddTiming(int parallelCount, Timing timing)
            {
                lock(timings)
                {
                    timings.AddIfMissing(parallelCount,
                        (create) =>
                        {
                            create(new List<Timing>() { timing });
                            return true;
                        },
                        (timingList, dictionary, created) =>
                        {
                            timingList.Add(timing);
                            return true;
                        });
                }
            }

            #region TaskScheduler abstract methods

            /// <summary>
            /// For debugger support only, generates an enumerable of Task instances currently queued to the scheduler waiting to be executed.
            /// </summary>
            /// <returns>An enumerable that allows a debugger to traverse the tasks currently queued to this scheduler.</returns>
            /// <remarks>
            /// Throws NotSupportedException
            /// if this scheduler is unable to generate a list of queued tasks at this time.
            /// </remarks>
            protected override IEnumerable<Task> GetScheduledTasks()
            {
                bool lockTaken = false;
                try
                {
                    Monitor.TryEnter(_tasks, ref lockTaken);
                    if (lockTaken) return _tasks;
                    else throw new NotSupportedException();
                }
                finally
                {
                    if (lockTaken) Monitor.Exit(_tasks);
                }
            }

            protected override void QueueTask(Task task)
            {
                // Add the task to the list of tasks to be processed.  If there aren't enough 
                // delegates currently queued or running to process tasks, schedule another. 
                lock (_tasks)
                {
                    _tasks.AddLast(task);
                    if (this.ActiveParallelism <= this.MaxDegreeOfParallelism) // delegatesQueuedOrRunning < MaxDegreeOfParallelism)
                    {
                        NotifyThreadPoolOfPendingWork();
                    }
                }
            }
            
            protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
            {
                // If this thread isn't already processing a task, we don't support inlining
                if (!_currentThreadIsProcessingItems) return false;

                // If the task was previously queued, remove it from the queue
                if (taskWasPreviouslyQueued)
                    // Try to run the task. 
                    if (TryDequeue(task))
                        return base.TryExecuteTask(task);
                    else
                        return false;
                else
                    return base.TryExecuteTask(task);
            }

            #endregion

            // Inform the ThreadPool that there's work to be executed for this scheduler. 
            private void NotifyThreadPoolOfPendingWork()
            {
                ThreadPool.UnsafeQueueUserWorkItem(_ =>
                {
                    // Note that the current thread is now processing work items.
                    // This is necessary to enable inlining of tasks into this thread.
                    _currentThreadIsProcessingItems = true;
                    try
                    {
                        // Process all available items in the queue.
                        while (true)
                        {
                            Task item;
                            lock (_tasks)
                            {
                                // When there are no more items to be processed,
                                // note that we're done processing, and get out.
                                if (_tasks.Count == 0)
                                {
                                    --delegatesQueuedOrRunning;
                                    break;
                                }

                                // Get the next item from the queue
                                item = _tasks.First.Value;
                                _tasks.RemoveFirst();
                            }

                            // Execute the task we pulled out of the queue
                            base.TryExecuteTask(item);
                        }
                    }
                    // We're done processing items on the current thread
                    finally { _currentThreadIsProcessingItems = false; }
                }, null);
            }

            // Attempt to remove a previously scheduled task from the scheduler. 
            protected sealed override bool TryDequeue(Task task)
            {
                lock (_tasks)
                {
                    return _tasks.Remove(task);
                }
            }

            internal void AddTask(Task<T> task)
            {
                this.tasks.Add(task);
            }
        }

        public static IEnumerable<TResult> Throttle<TSource, TResult, TThrottle>(this IEnumerable<TSource> enumerable,
            Func<TSource, IManagePerformance<TThrottle>, TResult> selectKey,
            int initialBandwidth = 1)
        {
            var throttler = new PerformanceManager<TThrottle>();
            return enumerable
                .Select(item => selectKey(item, throttler));
        }

        public static IEnumerableAsync<TResult> ThrottleOld<TSource, TResult>(this IEnumerable<TSource> enumerable, Func<TSource, Task<TResult>> selectKey,
            int initialBandwidth = 1)
        {
            return enumerable.Select(item => item.AsTask()).AsyncEnumerable().Throttle(selectKey, initialBandwidth);
        }

        public static IEnumerableAsync<TResult> Throttle<TSource, TResult>(this IEnumerableAsync<TSource> enumerable, Func<TSource, Task<TResult>> selectKey,
            int initialBandwidth = 1)
        {
            var sequentializor = new object();
            var taskParallelCount = 0;
            var epoch = DateTime.UtcNow;
            
            var scheduler = new ThrottleScheduler<TResult>(2);
            // Create a TaskFactory and pass it our custom scheduler. 
            TaskFactory factory = new TaskFactory(scheduler);
            CancellationTokenSource cts = new CancellationTokenSource();
            
            return enumerable
                .Select(
                    async input => await await factory.StartNew(
                        () =>
                        {
                            int taskParallelCountStart;
                            lock (sequentializor)
                            {
                                taskParallelCount++;
                                taskParallelCountStart = taskParallelCount;
                            }

                            var stopWatch = new System.Diagnostics.Stopwatch();
                            var when = (DateTime.UtcNow - epoch);
                            stopWatch.Start();
                            var output = selectKey(input);
                            scheduler.AddTask(output);
                            stopWatch.Stop();
                            lock (sequentializor)
                            {
                                var taskParallelCountEnd = taskParallelCount;
                                taskParallelCount--;
                                var timing = new Timing
                                {
                                    when = when,
                                    duration = stopWatch.Elapsed,
                                };
                                var taskParallelCountAverage = (int)(((taskParallelCountStart + taskParallelCountEnd) * 0.5) + 0.5);
                                scheduler.AddTiming(taskParallelCountAverage, timing);
                            }
                            return output;
                        }, cts.Token))
              .Await();

        }

        

    }
}

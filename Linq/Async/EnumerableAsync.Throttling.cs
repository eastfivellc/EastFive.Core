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
    public interface IManagePerformance<T>
    {

    }

    public static partial class EnumerableAsync
    {


        private class PerformanceManager<T> : IManagePerformance<T>
        {

        }

        public static IEnumerableAsync<TSource> ThrottleBegin<TSource, TResult>(this IEnumerable<TSource> enumerable,
            out IManagePerformance<TResult> performanceManager)
        {
            performanceManager = new PerformanceManager<TResult>();
            return enumerable.Select(item => item.AsTask()).AsyncEnumerable();
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

        private class ThrottleScheduler : TaskScheduler
        {
            private Dictionary<int, List<Timing>> timings;
            Random rand = new Random();

            private int delegatesQueuedOrRunning;
            [ThreadStatic]
            private static bool _currentThreadIsProcessingItems;
            private readonly LinkedList<Task> _tasks = new LinkedList<Task>(); // protected by lock(_tasks)

            public ThrottleScheduler(int v)
            {
                this.timings = new Dictionary<int, List<Timing>>();
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
                    if (delegatesQueuedOrRunning < MaxDegreeOfParallelism)
                    {
                        ++delegatesQueuedOrRunning;
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
        }

        public static IEnumerableAsync<TResult> Throttle<TSource, TResult>(this IEnumerable<TSource> enumerable, Func<TSource, TResult> selectKey,
            int initialBandwidth = 1)
        {
            return enumerable.Select(item => item.AsTask()).AsyncEnumerable().Throttle(selectKey, initialBandwidth);
        }

        public static IEnumerableAsync<TResult> Throttle<TSource, TResult>(this IEnumerableAsync<TSource> enumerable, Func<TSource, TResult> selectKey,
            int initialBandwidth = 1)
        {
            var sequentializor = new object();
            var taskParallelCount = 0;
            var epoch = DateTime.UtcNow;
            
            var scheduler = new ThrottleScheduler(2);
            // Create a TaskFactory and pass it our custom scheduler. 
            TaskFactory factory = new TaskFactory(scheduler);
            TaskFactory factoryInner = new TaskFactory();
            CancellationTokenSource cts = new CancellationTokenSource();
            
            return enumerable
                .Select(
                    async input =>
                    {
                        return await await factory.StartNew<Task<TResult>>(
                            async () =>
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
                                var task = factoryInner.StartNew(() => selectKey(input));
                                var output = await task;
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
                            }, cts.Token);
                    })
              .Await();

        }

        

    }
}

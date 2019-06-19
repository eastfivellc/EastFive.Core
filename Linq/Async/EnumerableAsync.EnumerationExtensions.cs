using EastFive.Collections.Generic;
using EastFive.Threading;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EastFive.Analytics;
using EastFive.Extensions;

namespace EastFive.Linq.Async
{
    public static partial class EnumerableAsync
    {
        public static Task<bool> AnyAsync<T>(this IEnumerableAsync<T> enumerable)
        {
            var enumerator = enumerable.GetEnumerator();
            return enumerator.MoveNextAsync();
        }

        public static Task<bool> ContainsAsync<T>(this IEnumerableAsync<T> enumerable, Func<T, bool> isMatchFunc)
        {
            return enumerable.FirstMatchAsync(
                (item, match, next) =>
                {
                    if (isMatchFunc(item))
                        return match(true);
                    return next();
                },
                () => false);
        }

        public static async Task<IEnumerable<T>> Async<T>(this IEnumerableAsync<T> enumerable,
            Analytics.ILogger logger = default(Analytics.ILogger))
        {
            var enumerator = enumerable.GetEnumerator();
            var firstStep = new Step<T>
            {
                current = default(T),
                steps = new Step<T>?[2],
            };
            var step = firstStep;
            logger.Trace("Async:First step");
            while (await enumerator.MoveNextAsync())
            {
                logger.Trace("Async:Moved next step");
                var nextStep = new Step<T>
                {
                    current = enumerator.Current,
                    steps = new Step<T>?[2],
                };
                nextStep.steps[StepEnumerable<T>.StepEnumerator.lastIndex] = step;
                step.steps[StepEnumerable<T>.StepEnumerator.nextIndex] = nextStep;
                step = nextStep;
            }
            logger.Trace("Async:Last step");
            return new StepEnumerable<T>(firstStep);
        }

        public static async Task<T[]> ToArrayAsync<T>(this IEnumerableAsync<T> enumerableAsync,
            EastFive.Analytics.ILogger logger = default(Analytics.ILogger))
        {
            var enumerable = await enumerableAsync.Async(logger);
            var output =  enumerable.ToArray();

            return output;
        }

        public static async Task<TResult> ToArrayAsync<T, TResult>(this IEnumerableAsync<T> enumerableAsync,
            Func<T[], TResult> onComplete)
        {
            var enumerable = await enumerableAsync.Async();
            var items = enumerable.ToArray();
            return onComplete(items);
        }

        public static async Task<IDictionary<TKey, TValue>> ToDictionaryAsync<TKey, TValue>(this IEnumerableAsync<KeyValuePair<TKey, TValue>> enumerableAsync)
        {
            var enumerable = await enumerableAsync.Async();
            return enumerable.ToDictionary();
        }

        public static Task<TResult> ToDictionaryAsync<TKey, TValue, TResult>(this IEnumerableAsync<KeyValuePair<TKey, TValue>> kvpItems,
            Func<Dictionary<TKey, TValue>, KeyValuePair<TKey, TValue>[], TResult> dictionaryAndDuplicates)
        {
            return kvpItems.ToDictionaryAsync(
                kvp => kvp.Key,
                kvp => kvp.Value,
                dictionaryAndDuplicates);
        }

        public static async Task<TResult> ToDictionaryAsync<TKey, TValue, TKeyDictionary, TValueDictionary, TResult>(this IEnumerableAsync<KeyValuePair<TKey, TValue>> kvpItems,
            Func<KeyValuePair<TKey, TValue>, TKeyDictionary> selectKey,
            Func<KeyValuePair<TKey, TValue>, TValueDictionary> selectValue,
            Func<Dictionary<TKeyDictionary, TValueDictionary>, KeyValuePair<TKey, TValue>[], TResult> dictionaryAndDuplicates)
        {
            var hashSet = new HashSet<TKey>();
            var kvpItemsArray = await kvpItems.ToArrayAsync();
            var duplicates = new KeyValuePair<TKey, TValue>[] { };
            var dictionary = kvpItemsArray
                .Select(
                    kvp =>
                    {
                        if (hashSet.Contains(kvp.Key))
                        {
                            duplicates = duplicates.Append(kvp).ToArray();
                            return default(KeyValuePair<TKeyDictionary, TValueDictionary>?);
                        }
                        hashSet.Add(kvp.Key);
                        return selectValue(kvp).PairWithKey(selectKey(kvp));
                    })
                .SelectWhereHasValue()
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            return dictionaryAndDuplicates(dictionary, duplicates);
        }

        public static async Task<List<T>> ToListAsync<T>(this IEnumerableAsync<T> enumerableAsync)
        {
            var enumerable = await enumerableAsync.Async();
            return enumerable.ToList();
        }

        public static async Task<ILookup<TKey, T>> ToLookupAsync<TKey, T>(this IEnumerableAsync<T> enumerableAsync,
            Func<T, TKey> keySelector)
        {
            var enumerable = await enumerableAsync.Async();
            return enumerable.ToLookup(keySelector);
        }

        public static IEnumerableAsync<T> JoinTask<T>(this IEnumerableAsync<T> enumerableAsync,
            Func<Task> task,
            EastFive.Analytics.ILogger logger = default(ILogger))
        {
            enumerableAsync
                .JoinTask(
                    Task.Run(task),
                    logger);
            return enumerableAsync;
        }

        public static IEnumerableAsync<T> JoinTask<T>(this IEnumerableAsync<T> enumerableAsync,
            Task task,
            EastFive.Analytics.ILogger logger = default(ILogger))
        {
            var scopedLogger = logger.CreateScope($"Join[{task.Id}]");
            var enumerator = enumerableAsync.GetEnumerator();
            return EnumerableAsync.Yield<T>(
                async (next, last) =>
                {
                    //if (!tag.IsNullOrWhiteSpace())
                    //    Console.WriteLine($"Join[{tag}] MoveNextAsync.");
                    if (await enumerator.MoveNextAsync())
                    {
                        //if (!tag.IsNullOrWhiteSpace())
                        //    Console.WriteLine($"Join[{tag}] Passthrough on value.");
                        return next(enumerator.Current);
                    }

                    scopedLogger.Trace($"Joining Task.");
                    await task;
                    scopedLogger.Trace($"Complete.");
                    return last;
                });
        }
        
        public static IEnumerableAsync<T> OnComplete<T>(this IEnumerableAsync<T> enumerableAsync,
            Action<T[]> onComplete,
            string tag = default(string))
        {
            var enumerator = enumerableAsync.GetEnumerator();
            var stack = new Stack<T>();
            return EnumerableAsync.Yield<T>(
                async (next, last) =>
                {
                    //if (!tag.IsNullOrWhiteSpace())
                    //    Console.WriteLine($"Join[{tag}] MoveNextAsync.");
                    if (await enumerator.MoveNextAsync())
                    {
                        var current = enumerator.Current;
                        //if (!tag.IsNullOrWhiteSpace())
                        //    Console.WriteLine($"Join[{tag}] Passthrough on value.");
                        stack.Push(current);
                        return next(current);
                    }

                    var allValues = stack.ToArray();
                    if (!tag.IsNullOrWhiteSpace())
                        Console.WriteLine($"OnComplete[{tag}] Accumulated `{allValues.Length}` Values.");
                    onComplete(allValues);
                    if (!tag.IsNullOrWhiteSpace())
                        Console.WriteLine($"OnComplete[{tag}] Complete.");

                    return last;
                });
        }

        private class CompleteAllMutex<T>
        {
            public EventWaitHandle mutex;
            public T[] values;
        }

        public static void OnCompleteAll<T>(this IEnumerable<IEnumerableAsync<T>> enumerableAsyncs,
            Action<T[][]> onComplete,
            string tag = default(string))
        {
            var mutexes = enumerableAsyncs
                .Select(
                    enumerableAsync =>
                    {
                        var s = new CompleteAllMutex<T>
                        {
                            mutex = new ManualResetEvent(false),
                            values = default(T[]),
                        };
                        enumerableAsync.OnComplete(
                            (values) =>
                            {
                                s.values = values;
                                s.mutex.Set();
                            });
                        return s;
                    })
                .ToArray();

            mutexes.Select(mutex => mutex.mutex).WaitAll();

            var allValuess = mutexes.Select(mutex => mutex.values).ToArray();
            onComplete(allValuess);
        }

        private struct Step<T>
        {
            public Step<T>?[] steps;
            public T current;
        }

        private class StepEnumerable<T> : IEnumerable<T>
        {
            internal class StepEnumerator : IEnumerator<T>
            {
                internal const int lastIndex = 0;
                internal const int nextIndex = 1;

                protected Step<T> current;
                public StepEnumerator(Step<T> current)
                {
                    this.current = current;
                }

                public T Current => current.current;

                object IEnumerator.Current => current.current;

                public void Dispose()
                {
                }

                public bool MoveNext()
                {
                    if (!current.steps[nextIndex].HasValue)
                        return false;
                    current = current.steps[nextIndex].Value;
                    return true;
                }

                public void Reset()
                {
                    while (current.steps[lastIndex].HasValue)
                        current = current.steps[lastIndex].Value;
                }
            }
            
            private Step<T> firstStep;
            public StepEnumerable(Step<T> firstStep)
            {
                this.firstStep = firstStep;
            }

            public IEnumerator<T> GetEnumerator()
            {
                return new StepEnumerator(firstStep);
            }

            IEnumerator IEnumerable.GetEnumerator() =>
                this.GetEnumerator();
        }
        
        
    }
}

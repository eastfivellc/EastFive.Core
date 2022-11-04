using System.Collections.Generic;
using BlackBarLabs.Linq;
using System.Threading.Tasks;
using BlackBarLabs.Extensions;
using EastFive.Linq;

namespace EastFive
{
    public static class TaskExtensions
    {
        public static async Task<IDictionary<TKey, TValue>> CastToGeneric<TKey, TValue>(this Task<Dictionary<TKey, TValue>> task)
        {
            var cast = await task;
            return cast;
        }

        public static async Task<T[]> WhenAllAsync2<T>(this IEnumerable<Task<T>> tasks, int parallelLimit = 0)
        {
            if (parallelLimit <= 0)
                return await Task.WhenAll(tasks);

            // TODO: Change this to use a semaphore and linq Aggregate; also move to EastFive namespace
            var results = await tasks.Aggregate((
                    new
                    {
                        queue = new List<Task<T>>(parallelLimit),
                        results = new List<T>(),
                    }).ToTask(),
                async (aggrTask, current, next) =>
                {
                    var aggr = await aggrTask;
                    aggr.queue.Add(current);
                    if (aggr.queue.Count >= parallelLimit)
                    {
                        var completedTask = await Task.WhenAny(aggr.queue);
                        aggr.queue.Remove(completedTask);
                        aggr.results.Add(await completedTask);
                    }
                    return await next(aggr.ToTask());
                },
                async (aggrTask) =>
                {
                    var aggr = await aggrTask;
                    aggr.results.AddRange(await Task.WhenAll(aggr.queue));
                    return aggr.results.ToArray();
                });

            return results;
        }

        public static async Task<T[]> WhenAllAsync<T>(this Task<IEnumerable<Task<T>>> tasksTask, int parallelLimit = 0)
        {
            var tasks = await tasksTask;
            return await TaskExtensions.WhenAllAsync(tasks, parallelLimit);
        }

        public static async Task RunAllAsync(this IEnumerable<Task> tasks, int parallelLimit = 0)
        {
            if (parallelLimit <= 0)
            {
                await Task.WhenAll(tasks);
                return;
            }

            var queue = new List<Task>(parallelLimit);
            foreach (var task in tasks)
            {
                queue.Add(task);
                if (queue.Count >= parallelLimit)
                {
                    var completedTask = await Task.WhenAny(queue.ToArray());
                    queue.Remove(completedTask);
                }
            }
            await Task.WhenAll(queue);
        }

        public static async Task<T[]> WhenAllAsync<T>(this IEnumerable<Task<T>> tasks, int parallelLimit = 0)
        {
            if (parallelLimit <= 0)
                return await Task.WhenAll(tasks);

            var results = new List<T>();
            var queue = new List<Task<T>>(parallelLimit);
            foreach (var task in tasks)
            {
                queue.Add(task);
                if (queue.Count >= parallelLimit)
                {
                    var completedTask = await Task.WhenAny(queue.ToArray());
                    queue.Remove(completedTask);
                    results.Add(await completedTask);
                }
            }
            results.AddRange(await Task.WhenAll(queue));
            return results.ToArray();
        }

    }
}

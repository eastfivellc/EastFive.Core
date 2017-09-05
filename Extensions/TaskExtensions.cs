using System.Collections.Generic;
using System.Threading.Tasks;

namespace BlackBarLabs
{
    public static class TaskExtensions
    {
        public static async Task<T[]> WhenAllAsync<T>(this IEnumerable<Task<T>> tasks, int parallelLimit = 0)
        {
            if(parallelLimit <= 0)
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

        public static async Task<T[]> WhenAllAsync<T>(this Task<IEnumerable<Task<T>>> tasksTask, int parallelLimit = 0)
        {
            var tasks = await tasksTask;
            return await tasks.WhenAllAsync(parallelLimit);
        }

        public static async Task WhenAllAsync(this IEnumerable<Task> tasks, int parallelLimit = 0)
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
    }
}

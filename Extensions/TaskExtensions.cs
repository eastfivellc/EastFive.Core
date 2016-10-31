using BlackBarLabs.Collections.Generic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlackBarLabs.Core
{
    public static class TaskExtensions
    {
        public static async Task<T[]> WhenAllAsync<T>(this IEnumerable<Task<T>> tasks, int parallelLimit = 0)
        {
            if(parallelLimit <= 0)
                return await Task.WhenAll(tasks);
            IEnumerable<T> results = new T[] { };
            foreach (var task in tasks)
            {
                var item = await task;
                results = results.Append(item);
            }
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
            foreach (var task in tasks)
            {
                await task;
            }
        }
    }
}

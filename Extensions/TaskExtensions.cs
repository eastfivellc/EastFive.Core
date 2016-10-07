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
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlackBarLabs.Collections.Generic
{
    public static class Enumerable
    {
        public static IEnumerable<T> CreateYield<T>(Action<Action<T>> yield)
        {
            return new T[] { }.AppendYield(yield);
        }

        public static async Task<IEnumerable<T>> CreateYieldAsync<T>(Func<Action<T>, Task> yield)
        {
            return await new T[] { }.AppendYieldAsync(yield);
        }
    }
}

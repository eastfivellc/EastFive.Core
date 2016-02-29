using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlackBarLabs.Collections.Async
{
    public static class LinqExtensions
    {
        public static async Task SelectAsync<T>(this IEnumerableAsync<T> items, T action)
        {
            var enumerator = items.GetEnumerator();
            while (await enumerator.MoveNextAsync(action)) { };
        }

        public static async Task<bool> First<T>(this IEnumerableAsync<T> items, T action)
        {
            var enumerator = items.GetEnumerator();
            return await enumerator.MoveNextAsync(action);
        }
    }
}

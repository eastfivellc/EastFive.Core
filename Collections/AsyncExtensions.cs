using BlackBarLabs.Collections.Generic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlackBarLabs.Collections.Async
{
    /// <summary>
    /// Why Microsoft, why make us build this!?!?!?!
    /// </summary>
    public static class AsyncExtensions
    {
        public static async Task<IEnumerable<T>> SelectManyAsync<T>(this Task<IEnumerable<IEnumerable<T>>> itemsTask)
        {
            var items = await itemsTask;
            return items.SelectMany();
        }

        public static async Task<IEnumerable<T>> SelectManyAsync<T>(this Task<IEnumerable<T[]>> itemsTask)
        {
            var items = await itemsTask;
            return items.SelectMany();
        }

        public static async Task<IEnumerable<T>> SelectManyAsync<T>(this Task<T[][]> itemsTask)
        {
            var items = await itemsTask;
            return items.SelectMany();
        }

        public static async Task<IEnumerable<T>> SelectManyAsync<T>(this Task<IEnumerable<T>[]> itemsTask)
        {
            var items = await itemsTask;
            return items.SelectMany();
        }

        public static async Task<T[]> ToArrayAsync<T>(this Task<IEnumerable<T>> itemsTask)
        {
            var items = await itemsTask;
            return items.ToArray();
        }

        public static async Task<IEnumerable<T>> WhereAsync<T>(this Task<IEnumerable<T>> itemsTask, Func<T, bool> predicate)
        {
            var items = await itemsTask;
            return items.Where(predicate);
        }

        public static async Task<IEnumerable<TResult>> SelectAsync<T1, TResult>(this Task<IEnumerable<T1>> itemsTask, Func<T1, TResult> selector)
        {
            var items = await itemsTask;
            return items.Select(selector);
        }

        public static async Task<IEnumerable<T>> SelectWhereHasValueAsync<T>(this Task<IEnumerable<T?>> itemsTask)
            where T : struct
        {
            var items = await itemsTask;
            return items.SelectWhereHasValue();
        }

        public static async Task<IEnumerable<T>> SelectWhereHasValueAsync<T>(this Task<T?[]> itemsTask)
            where T : struct
        {
            var items = await itemsTask;
            return items.SelectWhereHasValue();
        }
    }
}

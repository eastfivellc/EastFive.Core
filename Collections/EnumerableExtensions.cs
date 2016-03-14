using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlackBarLabs.Collections.Generic
{
    public static class EnumerableExtensions
    {
        public static IEnumerable<T> Append<T>(this IEnumerable<T> items, T item)
        {
            return items.Concat(new T[] { item });
        }

        public static IEnumerable<T> AppendYield<T>(this IEnumerable<T> items, Action<Action<T>> callback)
        {
            //foreach (var item in items)
            //    yield return item;

            var appendItems = new List<T>();
            callback((item) =>
            {
                appendItems.Add(item);
            });
            return items.Concat(appendItems);
        }
    }
}

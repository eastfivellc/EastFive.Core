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

        public static IEnumerable<T> RemoveItemAtIndex<T>(this IEnumerable<T> items, int index)
        {
            int indexOfItem = 0;
            foreach(var item in items)
            {
                if (index != indexOfItem)
                    yield return item;
                indexOfItem++;
            }
        }

        public static IEnumerable<T> SelectMany<T>(this IEnumerable<IEnumerable<T>> itemss)
        {
            return itemss.SelectMany(items => items);
        }

        public static async Task<IEnumerable<T>> AppendYieldAsync<T>(this IEnumerable<T> items, Func<Action<T>, Task> callback)
        {
            var appendItems = new List<T>();
            await callback((item) =>
            {
                appendItems.Add(item);
            });
            return items.Concat(appendItems);
        }

        public static TResult Min<TItem, TComparable, TResult>(this IEnumerable<TItem> items,
            Func<TItem, TComparable> sortCriteria,
            Func<TComparable, TComparable, int> comparer,
            Func<TItem, TResult> success,
            Func<TResult> emptyItems)
        {
            var enumerator = items.GetEnumerator();
            if (!enumerator.MoveNext())
                return emptyItems();
            var min = sortCriteria(enumerator.Current);
            var current = enumerator.Current;
            while (enumerator.MoveNext())
            {
                var minCandidate = sortCriteria(enumerator.Current);
                if (comparer(minCandidate, min) < 0)
                {
                    min = minCandidate;
                    current = enumerator.Current;
                }
            }
            return success(current);
        }

        public static TResult Min<TItem, TResult>(this IEnumerable<TItem> items,
            Func<TItem, long> sortCriteria,
            Func<TItem, TResult> success,
            Func<TResult> emptyItems)
        {
            return items.Min(sortCriteria,
                (long a, long b) => a < b ? -1 : (a == b ? 0 : 1),
                success, emptyItems);
        }
        
        public static TResult Max<TItem, TResult>(this IEnumerable<TItem> items,
            Func<TItem, long> sortCriteria,
            Func<TItem, TResult> success,
            Func<TResult> emptyItems)
        {
            return items.Min(sortCriteria,
                (long a, long b) => a > b ? -1 : (a == b ? 0 : 1),
                success, emptyItems);
        }

        public static TResult IndexOf<TItem, TResult>(this IEnumerable<TItem> items,
            TItem item,
            Func<TItem, TItem, bool> areEqual,
            Func<int, TResult> success,
            Func<TResult> notFound)
        {
            int index = 0;
            var enumerator = items.GetEnumerator();
            while (enumerator.MoveNext())
            {
                if (areEqual(enumerator.Current, item))
                    return success(index);
                index++;
            }
            return notFound();
        }

        public static TResult IndexOf<TResult>(this IEnumerable<Guid> items,
            Guid item,
            Func<int, TResult> success,
            Func<TResult> notFound)
        {
            return items.IndexOf(item, (item1, item2) => item1 == item2, success, notFound);
        }

        public static IEnumerable<T> ToEndlessLoop<T>(this IEnumerable<T> items)
        {
            bool operated = false;
            while(true)
            {
                foreach (var item in items)
                {
                    operated = true;
                    yield return item;
                }
                if (!operated)
                    break;
            }
        }

        public static IEnumerable<TReturn> Times<T, TReturn>(this IEnumerable<T> items, decimal total, decimal increments, Func<T, TReturn> action)
        {
            var itemsEnumerator = items.GetEnumerator();
            while (total > 0.0m)
            {
                itemsEnumerator.MoveNext();
                yield return action(itemsEnumerator.Current);
                total -= increments;
            }
        }

        public static bool Contains<T>(this IEnumerable<T> items, Func<T, bool> doesContain)
        {
            foreach(var item in items)
            {
                if (doesContain(item))
                    return true;
            }
            return false;
        }

        public static IEnumerable<IEnumerable<T>> Split<T>(this IEnumerable<T> items, Func<int, int> batchSizeCallback)
        {
            var itemsCopy = items;
            var index = 0;
            while (itemsCopy.Any())
            {
                var batchsize = batchSizeCallback(index);
                yield return itemsCopy.Take(batchsize);
                itemsCopy = itemsCopy.Skip(batchsize);
                index += batchsize;
            }
        }
        
        public static IEnumerable<TResult> Select<TSource, TResult>(
            this IEnumerable<TSource> source,
            Func<TSource, int, TResult> selector)
        {
            int index = 0;
            foreach (var item in source.NullToEmpty())
            {
                yield return selector(item, index);
                index++;
            }
        }

        public static IEnumerable<TSource> NullToEmpty<TSource>(
            this IEnumerable<TSource> source)
        {
            if (default(IEnumerable<TSource>) == source)
                return new TSource[] { };
            return source;
        }
    }
}

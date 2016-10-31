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
            foreach(var item in items.NullToEmpty())
            {
                if (doesContain(item))
                    return true;
            }
            return false;
        }

        public static IEnumerable<IEnumerable<T>> Split<T>(this IEnumerable<T> items, Func<int, int> batchSizeCallback)
        {
            var itemsCopy = items.NullToEmpty();
            var index = 0;
            while (itemsCopy.Any())
            {
                var batchsize = batchSizeCallback(index);
                yield return itemsCopy.Take(batchsize);
                itemsCopy = itemsCopy.Skip(batchsize);
                index += batchsize;
            }
        }

        public static IEnumerable<TSource> NullToEmpty<TSource>(
            this IEnumerable<TSource> source)
        {
            if (default(IEnumerable<TSource>) == source)
                return new TSource[] { };
            return source;
        }
        
        public static T Random<T>(this IEnumerable<T> items, int total, Random rand = null)
        {
            if (rand == null)
            {
                rand = new Random();
            }
            var totalD = (double)total;
            var arrayItems = new T[total];
            var arrayItemsIndex = 0;
            foreach (var item in items)
            {
                if (rand.NextDouble() < (1.0 / totalD))
                {
                    return item;
                }
                totalD -= 1.0;
                arrayItems[arrayItemsIndex] = item;
                arrayItemsIndex++;
            }
            if (arrayItemsIndex == 0)
            {
                return default(T);
            }
            var selectedIndex = (int)(arrayItemsIndex * rand.NextDouble());
            return arrayItems[selectedIndex];
        }

        public static T Random<T>(this IEnumerable<T> items, Random rand = null)
        {
            return items.Random(items.Count(), rand);
        }

        public static TResult FirstOrDefault<T, TResult>(this IEnumerable<T> items,
            Func<T, TResult> found,
            Func<TResult> notFound)
        {
            if (items.Any())
                return found(items.First());
            return notFound();
        }

        public static TResult FirstOrDefault<T, TResult>(this IEnumerable<T> items, Func<T, bool> predicate,
            Func<T, TResult> found,
            Func<TResult> notFound)
        {
            foreach (var item in items)
                if (predicate(item))
                    return found(item);
            return notFound();
        }

        public static TResult GetDistinctKvpValueByKey<TResult>(this IEnumerable<KeyValuePair<string, object>> kvps, string key,
            Func<object, TResult> found,
            Func<string, TResult> notFound,
            Func<string, TResult> multipleItemsFoundWithSameKey)
        {
            var value = kvps.Where(kvp => kvp.Key == key).ToList();
            if (!value.Any())
            {
                return notFound($"Could not find key {key}");
            }
            if (value.Count() > 1)
            {
                return multipleItemsFoundWithSameKey($"Multiple items found for key {key}");
            }
            return found(value.First().Value);
        }

        public delegate TResult SelectWithDelegate<TWith, TItem, TResult>(TWith previous, TItem current, out TWith next);
        public static IEnumerable<TResult> SelectWith<TWith, TItem, TResult>(this IEnumerable<TItem> items,
            TWith seed, SelectWithDelegate<TWith, TItem, TResult> callback)
        {
            var carry = seed;
            foreach(var item in items)
            {
                yield return callback(carry, item, out carry);
            }
        }

        public static IEnumerable<TItem> OrderWith<TCarry, TItem>(this IEnumerable<TItem> items,
            TCarry carry, Func<TCarry, TItem, bool> selectNextItem, Func<TItem, TCarry> selectNextCarry)
        {
            var itemsAvailable = items.NullToEmpty().ToArray();
            while(itemsAvailable.Any())
            {
                bool found = false;
                var nextItem = default(TItem);
                itemsAvailable = itemsAvailable.Where(candidate =>
                    {
                        if (!found && selectNextItem(carry, candidate))
                        {
                            nextItem = candidate;
                            return false;
                        }
                        return true;
                    })
                    .ToArray();
                if (!found)
                {
                    foreach (var item in items)
                        yield return item;
                    yield break;
                }
                yield return nextItem;
                carry = selectNextCarry(nextItem);
            }
        }

        public class SelectDiscriminateResult<TOption1, TOption2>
        {
            public TOption1 Option1;
            public TOption2 Option2;
            public bool IsOption2;

            internal SelectDiscriminateResult(TOption1 item) { Option1 = item; IsOption2 = false; }
            internal SelectDiscriminateResult(TOption2 item) { Option2 = item; IsOption2 = true; }
        }

        public static TResult SelectDiscriminate<TItem, TOption1, TOption2, TResult>(this IEnumerable<TItem> items,
                Func<
                    TItem,
                    Func<TOption1, SelectDiscriminateResult<TOption1, TOption2>>,
                    Func<TOption2, SelectDiscriminateResult<TOption1, TOption2>>,
                    SelectDiscriminateResult<TOption1, TOption2>> callback,
            Func<IEnumerable<TOption1>, TResult> option1,
            Func<TOption2, TResult> option2)
        {
            IEnumerable<TOption1> option1s = new TOption1[] { };
            foreach(var item in items)
            {
                var dr = callback(item,
                    r => new SelectDiscriminateResult<TOption1, TOption2>(r),
                    f => new SelectDiscriminateResult<TOption1, TOption2>(f));
                if (dr.IsOption2)
                    return option2(dr.Option2);
                option1s = option1s.Append(dr.Option1);
            }
            return option1(option1s);
        }

        public static IEnumerable<TResult> SplitSelect<TItem, TResult>(this IEnumerable<TItem> items, Predicate<TItem> isOption1,
            Func<TItem, TResult> operation1, Func<TItem, TResult> operation2)
        {
            foreach (var item in items)
                if (isOption1(item))
                    yield return operation1(item);
                else
                    yield return operation2(item);
        }
    }
}

using BlackBarLabs.Collections.Generic;
using BlackBarLabs.Extensions;
using EastFive.Collections.Generic;
using EastFive.Extensions;
using EastFive.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EastFive.Linq
{
    public static class EnumerableExtensions
    {
        public static IEnumerable<TSource> NullToEmpty<TSource>(
            this IEnumerable<TSource> source)
        {
            if (default(IEnumerable<TSource>) == source)
                return new TSource[] { };
            return source;
        }

        public static IEnumerable<T> Distinct<T>(this IEnumerable<T> items,
            Func<T, string> propertySelection)
        {
            Func<T, T, int> comparer = (v1, v2) =>
                String.Compare(propertySelection(v1), propertySelection(v2));
            return EnumerableExtensions.Distinct(items, comparer,
                v => propertySelection(v).GetHashCode());
        }

        public static IEnumerable<T> Distinct<T>(this IEnumerable<T> items,
            Func<T, Guid> propertySelection)
        {
            Func<T, T, bool> comparer = (v1, v2) =>
                propertySelection(v1) == propertySelection(v2);
            return EnumerableExtensions.Distinct(items, comparer,
                v => propertySelection(v).GetHashCode());
        }

        public static IEnumerable<KeyValuePair<string, T>> Distinct<T>(this IEnumerable<KeyValuePair<string, T>> items)
        {
            return items.Distinct(item => item.Key);
        }

        public static IEnumerable<KeyValuePair<Guid, T>> Distinct<T>(this IEnumerable<KeyValuePair<Guid, T>> items)
        {
            return items.Distinct(item => item.Key);
        }

        public static T[] Duplicates<T>(this IEnumerable<T> items, Func<T, T, bool> predicate)
        {
            var array = items.ToArray();
            return array
                .Aggregate(
                    new T[] { },
                    (aggr, item) =>
                    {
                        // See if this is already matched
                        if (aggr.Any(aggrSet => predicate(aggrSet, item)))
                            return aggr;

                        var matches = array.Where(arrayItem => predicate(arrayItem, item)).ToArray();
                        return matches.Length > 1 ?
                            aggr.Append(item).ToArray()
                            :
                            aggr;
                    });
        }

        public static bool All(this IEnumerable<bool> items)
        {
            return items.All(b => b);
        }
        
        public static IEnumerable<T> Append<T>(this IEnumerable<T> items, T item)
        {
            return items.NullToEmpty().Concat(new T[] { item });
        }

        public static IEnumerable<T> AppendIf<T>(this IEnumerable<T> items, T item, bool condition)
        {
            if (!condition)
                return items;
            return items.Append(item);
        }

        public static IEnumerable<T> AppendIf<T>(this IEnumerable<T> items, Func<T> getItem, bool condition)
        {
            if (!condition)
                return items;
            return items.Append(getItem());
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
        
        public static IEnumerable<T> Distinct<T>(this IEnumerable<T> items, 
            Func<T, T, int> comparer,
            Func<T, int> hash = default(Func<T, int>))
        {
            Func<T, T, bool> predicateComparer = (v1, v2) => comparer(v1, v2) == 0;
            return items.Distinct(predicateComparer, hash);
        }

        public static IEnumerable<T> Distinct<T>(this IEnumerable<T> items, 
            Func<T, T, bool> predicateComparer,
            Func<T, int> hash = default(Func<T, int>))
        {
            IEqualityComparer<T> comparer = predicateComparer.ToEqualityComparer(hash);
            return items.Distinct(comparer);
        }

        public static IEnumerable<T> Except<T>(this IEnumerable<T> items, IEnumerable<T> itemsToExclude,
            Func<T, T, int> comparer,
            Func<T, int> hash = default(Func<T, int>))
        {
            IEqualityComparer<T> x = comparer.ToEqualityComparer(hash);
            return items.Except(itemsToExclude, x);
        }

        public static IEnumerable<T> Except<T>(this IEnumerable<T> items, IEnumerable<T> itemsToExclude, 
            Func<T, T, bool> predicateComparer,
            Func<T, int> hash = default(Func<T, int>))
        {
            IEqualityComparer<T> comparer = predicateComparer.ToEqualityComparer(hash);
            return items.Except(itemsToExclude, comparer);
        }

        public static IEnumerable<T> Except<T>(this IEnumerable<T> items, IEnumerable<T> itemsToExclude,
            Func<T, string> propertySelection,
            Func<T, int> hash = default(Func<T, int>))
        {
            Func<T, T, int> comparer = (v1, v2) =>
                   String.Compare(propertySelection(v1), propertySelection(v2));
            return items.Except(itemsToExclude, comparer,
                v => propertySelection(v).GetHashCode());
        }

        public static IEnumerable<T> Except<T>(this IEnumerable<T> items, IEnumerable<T> itemsToExclude,
            Func<T, Guid> propertySelection,
            Func<T, int> hash = default(Func<T, int>))
        {
            Func<T, T, bool> comparer = (v1, v2) =>
                propertySelection(v1) == propertySelection(v2);
            return items.Except(itemsToExclude, comparer,
                v => propertySelection(v).GetHashCode());
        }

        public static IEnumerable<T> Intersect<T>(this IEnumerable<T> items, IEnumerable<T> second,
            Func<T, T, int> comparer,
            Func<T, int> hash = default(Func<T, int>))
        {
            Func<T, T, bool> predicateComparer = (v1, v2) => comparer(v1, v2) == 0;
            return items.Intersect(second, predicateComparer, hash);
        }

        public static IEnumerable<T> Intersect<T>(this IEnumerable<T> items, IEnumerable<T> second,
            Func<T, T, bool> predicateComparer,
            Func<T, int> hash = default(Func<T, int>))
        {
            IEqualityComparer<T> comparer = predicateComparer.ToEqualityComparer(hash);
            return items.Intersect(second, comparer);
        }

        public static IEnumerable<T> Intersect<T>(this IEnumerable<T> items, IEnumerable<T> second,
            Func<T, string> propertySelection,
            Func<T, int> hash = default(Func<T, int>))
        {
            Func<T, T, int> comparer = (v1, v2) =>
                   String.Compare(propertySelection(v1), propertySelection(v2));
            return items.Intersect(second, comparer,
                v => propertySelection(v).GetHashCode());
        }

        public static TResult Merge<T0, T1, T2, TResult>(this IEnumerable<T1> items1, 
                IEnumerable<T2> items2,
                Func<T1, T0> propertySelection1,
                Func<T2, T0> propertySelection2,
            Func<IDictionary<T0, KeyValuePair<T1, T2>>, IEnumerable<T1>, IEnumerable<T2>, TResult> onResult,
            Func<T0, T0, bool> predicateComparer,
            Func<T0, int> hash = default(Func<T0, int>))
        {
            var item1Lookup =
                items1.Select(item => propertySelection1(item).PairWithValue(item))
                .ToDictionary();
            var item2Lookup =
                items2.Select(item => propertySelection2(item).PairWithValue(item))
                .ToDictionary();
            var item1Keys = item1Lookup.SelectKeys();
            var item2Keys = item2Lookup.SelectKeys();
            var intersection = item1Keys
                .Intersect(item2Keys, predicateComparer, hash);

            var itemsIntesection = intersection
                .Select(key => key.PairWithValue(
                    item1Lookup[key].PairWithValue(item2Lookup.First(item => predicateComparer(key, item.Key)).Value)))
                .ToDictionary();

            var items1Unmatched = item1Lookup
                .SelectKeys()
                .Except(intersection, predicateComparer, hash)
                .Select(unmatchedKey => item1Lookup[unmatchedKey]);
            var items2Unmatched = item2Lookup
                .SelectKeys()
                .Except(intersection, predicateComparer, hash)
                .Select(unmatchedKey => item2Lookup[unmatchedKey]);

            return onResult(itemsIntesection, items1Unmatched, items2Unmatched);
        }


        public static IDictionary<TKey, TValue> Merge<TKey, TValue>(this IDictionary<TKey, TValue> items1,
            IEnumerable<KeyValuePair<TKey, TValue>> items2,
            Func<TKey, TValue, TValue, TValue> merge)
        {
            var item1Lookup = items1.NullToEmpty().ToDictionary();
            var item2Lookup = items2.NullToEmpty().ToDictionary();
            var item1Keys = item1Lookup.SelectKeys();
            var item2Keys = item2Lookup.SelectKeys();

            var intersection = item1Keys.Union(item2Keys);

            return intersection
                .Select(key => key.PairWithValue(
                    item1Lookup.ContainsKey(key) ?
                        item2Lookup.ContainsKey(key) ?
                            merge(key, item1Lookup[key], item2Lookup[key])
                        :
                        item1Lookup[key]
                    :
                    item2Lookup[key]))
                .ToDictionary();
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

        public static IEnumerable<TResult> Select<TItem, TResult>(this IEnumerable<TItem> items,
             Func<TItem, TResult> single, Func<TItem, TItem, TResult> first, Func<TItem, TItem, TItem, TResult> middle, Func<TItem, TItem, TResult> last)
        {
            var iter = items.GetEnumerator();
            if (!iter.MoveNext())
            {
                yield break;
            }
            var lastValue = iter.Current;

            if (!iter.MoveNext())
            {
                yield return single(lastValue);
                yield break;
            }
            var current = iter.Current;

            yield return first(lastValue, current);

            while(iter.MoveNext())
            {
                yield return middle(lastValue, current, iter.Current);
                lastValue = current;
                current = iter.Current;
            }
            yield return last(lastValue, current);
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

        public static IEnumerable<TResult> SelectWithPeek<TItem, TResult, TInnerResult>(this IEnumerable<TItem> items,
            Func<
                TItem,
                Func<
                    Func<TItem, TInnerResult>,
                    Func<TInnerResult>,
                    TInnerResult>,
                TResult> callback)
        {
            var enumerator = items.GetEnumerator();
            bool peeked = false;
            while(peeked || enumerator.MoveNext())
            {
                yield return callback(enumerator.Current,
                    (next, end) =>
                    {
                        peeked = true;
                        return enumerator.MoveNext() ?
                            next(enumerator.Current)
                            :
                            end();
                    });
            }
        }

        public interface ISelected<T>
        {
            bool HasValue { get; }
            T Value { get; }
        }

        public struct SelectedValue<T> : ISelected<T>
        {
            public SelectedValue(bool x)
            {
                this.HasValue = false;
                this.Value = default(T);
            }

            public SelectedValue(T nextItem)
            {
                this.HasValue = true;
                this.Value = nextItem;
            }

            public bool HasValue {get; private set;}

            public T Value { get; private set; }
        }

        public static IEnumerable<TResult> SelectOptional<TItem, TResult>(this IEnumerable<TItem> items,
            Func<TItem, Func<TResult, ISelected<TResult>>, Func<ISelected<TResult>>, ISelected<TResult>> callback)
        {
            foreach (var item in items)
            {
                var nextValue = callback(item,
                    (nextItem) => new SelectedValue<TResult>(nextItem),
                    () => new SelectedValue<TResult>(false));
                if (nextValue.HasValue)
                    yield return nextValue.Value;
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

        public static async Task<TResult> SelectDiscriminateAsync<TItem, TOption1, TOption2, TResult>(this IEnumerable<TItem> items,
                Func<
                    TItem,
                    Func<TOption1, SelectDiscriminateResult<TOption1, TOption2>>,
                    Func<TOption2, SelectDiscriminateResult<TOption1, TOption2>>,
                    Task<SelectDiscriminateResult<TOption1, TOption2>>> callback,
            Func<IEnumerable<TOption1>, TResult> option1,
            Func<TOption2, TResult> option2)
        {
            IEnumerable<TOption1> option1s = new TOption1[] { };
            foreach (var item in items)
            {
                var dr = await callback(item,
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


        public class SelectUntilResult<TOption1, TOption2>
        {
            public TOption1 Option1;
            public TOption2 Option2;
            public bool IsOption2;

            internal SelectUntilResult(TOption1 item) { Option1 = item; IsOption2 = false; }
            internal SelectUntilResult(TOption2 item) { Option2 = item; IsOption2 = true; }
        }

        public static TResult SelectUntil<TItem, TTransformed, TResult>(this IEnumerable<TItem> items,
                Func<
                    TItem,
                    Func<TTransformed, SelectUntilResult<TTransformed, TResult>>,
                    Func<TResult, SelectUntilResult<TTransformed, TResult>>,
                    SelectUntilResult<TTransformed, TResult>> callback,
            Func<TTransformed[], TResult> option1)
        {
            var option1s = new TTransformed[] { };
            foreach (var item in items)
            {
                var dr = callback(item,
                    r => new SelectUntilResult<TTransformed, TResult>(r),
                    f => new SelectUntilResult<TTransformed, TResult>(f));
                if (dr.IsOption2)
                    return dr.Option2;
                option1s = option1s.Append(dr.Option1).ToArray();
            }
            return option1(option1s);
        }

        public static IEnumerable<TItem> Until<TItem>(this IEnumerable<TItem> items,
                Func<TItem, bool> predicate)
        {
            foreach (var item in items)
            {
                if (!predicate(item))
                    yield break;
                yield return item;
            }
        }

        public static IEnumerable<T> SelectRandom<T>(this IEnumerable<T> items, int total, Random rand = null)
        {
            if (rand == null)
                rand = new Random();

            var totalD = (double)total;
            foreach (var item in items)
            {
                if (totalD < 0.5 ||
                    rand.NextDouble() < (1.0 / totalD))
                {
                    yield return item;
                }
                totalD -= 1.0;
            }
        }

        public static IEnumerable<T> SelectWhereHasValue<T>(this IEnumerable<Nullable<T>> items)
            where T : struct
        {
            return items
                .Where(item => item.HasValue)
                .Select(item => item.Value);
        }

        public static IEnumerable<T> SelectWhereNotNull<T>(this IEnumerable<T> items)
            where T : class
        {
            return items
                .Where(item => (!EqualityComparer<T>.Default.Equals(item, default(T))))
                .Select(item => item);
        }

        public static IEnumerable<KeyValuePair<T1, T2>> Combine<T1, T2>(this IEnumerable<T1> items1, IEnumerable<T2> combineWith)
        {
            foreach(var item1 in items1)
                foreach(var item2 in combineWith)
                    yield return item1.PairWithValue(item2);
        }

        public static T[][] Combinations<T>(this IEnumerable<T> items)
        {
            var itemsArray = items.ToArray();

            if (itemsArray.Length == 0)
                return new T[][] { };

            if (itemsArray.Length == 1)
                return new T[][] { itemsArray };

            var item = itemsArray[0];
            var remainder = items.Skip(1).ToArray();
            var combinationsRemainder = remainder
                .Combinations();
            var combinations = combinationsRemainder
                .Select(co => co.Append(item).ToArray())
                .Concat(combinationsRemainder)
                .Append(new[] { item })
                .ToArray();
            return combinations;
        }

        public static TAccumulate Aggregate<TSource, TAccumulate>(this IEnumerable<TSource> items,
            TAccumulate seed,
            Func<TAccumulate, TSource, int, TAccumulate> func)
        {
            return items.Aggregate(seed.PairWithKey(0),
                (accum, item) =>
                {
                    return func(accum.Value, item, accum.Key).PairWithKey(accum.Key + 1);
                }).Value;
        }

        public static TResult Aggregate<TItem, TAccum, TResult>(this IEnumerable<TItem> items,
            TAccum start,
            Func<TAccum, TItem, Func<TAccum, TResult>, TResult> aggr,
            Func<TAccum, TResult> onComplete)
        {
            var enumerator = items.GetEnumerator();
            var final = Aggregate(enumerator, start, aggr, onComplete);
            return (final);
        }

        private static TResult Aggregate<TItem, TAccum, TResult>(IEnumerator<TItem> items,
            TAccum start,
            Func<TAccum, TItem, Func<TAccum, TResult>, TResult> aggr,
            Func<TAccum, TResult> onComplete)
        {
            if (!items.MoveNext())
                return onComplete(start);
            return aggr(start, items.Current,
                (next) => Aggregate(items, next, aggr, onComplete));
        }
        
        public static TResult AggregateConsume<TItem, TResult>(this IEnumerable<TItem> items,
            Func<TItem, Func<TResult>, TResult> aggr,
            Func<TResult> noItems)
        {
            var enumerator = items.GetEnumerator();
            var final = AggregateConsume(enumerator, aggr, noItems);
            return (final);
        }

        private static TResult AggregateConsume<TItem, TResult>(IEnumerator<TItem> items,
            Func<TItem, Func<TResult>, TResult> aggr,
            Func<TResult> noItems)
        {
            if (!items.MoveNext())
                return noItems();
            return aggr(items.Current,
                () => AggregateConsume(items, aggr, noItems));
        }

        public static IEnumerable<TItem> TakeUntil<TItem>(this IEnumerable<TItem> items,
            Func<TItem, bool> predicate)
        {
            foreach (var item in items)
            {
                if (!predicate(item))
                    yield return item;
                else
                {
                    yield return item;
                    yield break;
                }
            }
        }

        public static IEnumerable<TItem> OrderByLink<TItem>(this IEnumerable<TItem> items,
            Func<TItem, bool> isFirst,
            Func<TItem, TItem, bool> isNext)
        {
            return items.FirstOrDefault(item => isFirst(item),
                (currentItem) => currentItem.AsArray().Concat(
                    items
                        .Where(item => !item.Equals(currentItem))
                        .OrderByLink(
                            (item) => isNext(currentItem, item),
                            isNext)),
                    () => new TItem[] { });
        }

        public static IEnumerable<TItem> OrderByLink<TItem, TKey>(this IEnumerator<TItem> items,
            Func<TItem, bool> isFirst,
            Func<TItem, TKey> getValue,
            Func<TItem, TKey> getLink)
        {
            if (!items.MoveNext())
                yield break;

        }

        public static IEnumerable<TItem> AsEnumerable<TItem>(this IEnumerator<TItem> items)
        {
            while (items.MoveNext())
                yield return items.Current;
        }

        public static bool ContainsEqual<TItem>(this IEnumerable<TItem> items1, IEnumerable<TItem> items2)
        {
            var items1Arr = items1.ToArray();
            var items2Arr = items2.ToArray();
            if (items1Arr.Count() != items2Arr.Count())
                return false;
            return items1Arr.All(item => items2Arr.Contains(item));
        }

        public static TResult Bucket<TItem1, TItem2, Selector1, Selector2, TResult>(this IEnumerable<TItem1> items1, IEnumerable<TItem2> items2,
            Func<TItem1, Selector1> selector1,
            Func<TItem2, Selector2> selector2,
            Func<Selector1, Selector2, bool> matchPredicate,
            Func<KeyValuePair<TItem1[], TItem2[]>[], TItem1 [], TItem2[], TResult> onGetResult)
        {
            var items1Grouped = items1.GroupBy(selector1);
            var items2Grouped = items2.GroupBy(selector2).ToArray();
            return items1Grouped.FlatMap<IGrouping<Selector1, TItem1>, TItem1[], IGrouping<Selector2, TItem2>[], KeyValuePair<TItem1[], TItem2[]>, Func<TResult>>(
                    new TItem1[] { }, items2Grouped,
                (item1, items1Unmatched, items2Unmatched, next, skip) =>
                {
                    return items2Unmatched.SplitReduce(
                        item2 => matchPredicate(item1.Key, item2.Key),
                        (items2Matching, items2UnmatchedNew) =>
                        {
                            if (!items2Matching.Any())
                                return skip(items1Unmatched.Concat(item1).ToArray(), items2Unmatched);
                            return next(
                                item1.ToArray().PairWithValue(items2Matching.Select(grp => grp).SelectMany().ToArray()),
                                items1Unmatched,
                                items2UnmatchedNew.ToArray());
                        });
                },
                (KeyValuePair<TItem1[], TItem2[]>[] itemsMatched, TItem1 [] items1Unmatched, IGrouping<Selector2, TItem2>[] items2Unmatched) =>
                {
                    // return fuction to not trigger an async FlatMap
                    return () => onGetResult(itemsMatched, items1Unmatched, items2Unmatched.Select(grp => grp.ToArray()).SelectMany().ToArray());
                })();
        }

        //public static bool SequenceEqual<TItem>(this IEnumerable<TItem> items1, IEnumerable<TItem> items2)
        //    where TItem : class
        //{
        //    return items1.Zip(items2, (i1, i2) => i1 == i2).All(b => b);
        //}
    }
}

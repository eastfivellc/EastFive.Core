using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using EastFive.Collections.Generic;
using EastFive.Extensions;
using EastFive.Linq;

namespace EastFive.Linq
{
    public static class EnumerableExtensions
    {
#if !NETCORE
        public static IEnumerable<T> Append<T>(this IEnumerable<T> items, T addition)
        {
            foreach (var item in items.NullToEmpty())
                yield return item;
            yield return addition;
        }
#endif

        public static IEnumerable<TSource> NullToEmpty<TSource>(
            this IEnumerable<TSource> source)
        {
            if (default(IEnumerable<TSource>) == source)
                return new TSource[] { };
            return source;
        }

        public static bool One<TSource>(
            this IEnumerable<TSource> source)
        {
            return source.NullToEmpty().Count() == 1;
        }

        public static bool Many<TSource>(
            this IEnumerable<TSource> source)
        {
            return source.NullToEmpty().Count() > 1;
        }

        public static IEnumerable<TSource> Less<TSource>(
            this IEnumerable<TSource> source, int less)
        {
            if (less < 0)
                throw new ArgumentException($"Less({less}) cannot be negative", "less");
            if (less == 0)
            {
                foreach (var item in source)
                    yield return item;
                yield break;
            }
            var enumerator = source.GetEnumerator();
            var queueAhead = new TSource[less];
            var index = 0;
            while (index < less)
            {
                if (!enumerator.MoveNext())
                    yield break;
                queueAhead[index] = enumerator.Current;
                index++;
            }
            var swapIndex = 0;
            while (enumerator.MoveNext())
            {
                yield return queueAhead[swapIndex];
                queueAhead[swapIndex] = enumerator.Current;
                swapIndex = (swapIndex + 1) % queueAhead.Length;
            }
        }


        public static IEnumerable<TSource> Less<TSource>(
            this IEnumerable<TSource> source, int less, out TSource[] lastItems)
        {
            if (less < 0)
                throw new ArgumentException($"Less({less}) cannot be negative", "less");
            lastItems = new TSource[less];
            var allItems = source.ToArray();
            Array.Copy(allItems, 0, lastItems, allItems.Length - less, less);
            return Enumerable
                .Range(0, allItems.Length - less)
                .Select(index => allItems[index]);
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

        public static IEnumerable<T> DistinctById<T>(this IEnumerable<T> items) 
            where T : IReferenceable
            => items.Distinct(item => item.id);

        public static IEnumerable<T> Distinct<T>(this IEnumerable<T> items,
                bool includeNull, bool nullEqualsDefault = true)
            where T : IReferenceableOptional
        {
            if (!includeNull)
                return items
                    .Where(item => item.HasValue)
                    .Distinct(item => item.id.Value);

            if(nullEqualsDefault)
                return items
                    .Distinct(
                        item => item.id.HasValue?
                            item.id.Value
                            :
                            Guid.Empty);

            var nullPlaceHolder = Guid.NewGuid();
            return items
                    .Distinct(
                        item => item.id.HasValue ?
                            item.id.Value
                            :
                            nullPlaceHolder);
        }

        public static IEnumerable<KeyValuePair<string, T>> Distinct<T>(this IEnumerable<KeyValuePair<string, T>> items)
        {
            return items.Distinct(item => item.Key);
        }

        public static IEnumerable<KeyValuePair<Guid, T>> Distinct<T>(this IEnumerable<KeyValuePair<Guid, T>> items)
        {
            return items.Distinct(item => item.Key);
        }

        public static IEnumerable<T> DistinctSets<T, TSetItem>(this IEnumerable<T> items,
            Func<T, TSetItem[]> breakSet)
        {
            var uniques = new HashSet<TSetItem>();

            foreach (var item in items)
            {
                var alreadyUsed = false;
                foreach (var setItem in breakSet(item))
                {
                    var itemAlreadyUsed = uniques.Contains(setItem);
                    if (!itemAlreadyUsed)
                        uniques.Add(setItem);
                    alreadyUsed = alreadyUsed || itemAlreadyUsed;
                }
                if (!alreadyUsed)
                    yield return item;
            }
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

        //public static IEnumerable<T> Append<T>(this IEnumerable<T> items, T item)
        //{
        //    return items.NullToEmpty().Concat(new T[] { item });
        //}

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
            foreach (var item in items)
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

        public static IEnumerable<T> Exclude<T>(this IEnumerable<T> items,
            T itemToExclude, int limit = int.MaxValue)
        {
            foreach (var item in items)
            {
                if (limit <= 0)
                {
                    yield return item;
                    continue;
                }
                if (!EqualityComparer<T>.Default.Equals(item, itemToExclude))
                {
                    yield return item;
                    continue;
                }
                limit--;
            }
        }

        public static IEnumerable<T1> Excludes<T1, T2>(this IEnumerable<T1> items1,
                IEnumerable<T2> items2,
                Func<T1, T2, bool> predicateComparer)
        {
            var items2Lookups = items2.ToArray();
            return items1
                .Where(
                    item1 => items2Lookups
                        .All(
                            (item2) =>
                            {
                                return !predicateComparer(item1, item2);
                            }));
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

        public static (T[], T[], T[]) Intersect<T>(this IEnumerable<T> items1, IEnumerable<T> items2,
            Func<T, int> hash)
        {
            var items1Dictionary = items1
                .Select(item => hash(item).PairWithValue(item))
                .ToDictionary();

            var items2Dictionary = items2
                .Select(item => hash(item).PairWithValue(item))
                .ToDictionary();

            var intersectingKeys = items1Dictionary.Keys
                .Intersect(items2Dictionary.Keys)
                .ToArray();

            var intersectingValues = intersectingKeys
                .Select(key => items1Dictionary[key])
                .ToArray();

            var item1Values = items1Dictionary.Keys
                .Except(intersectingKeys)
                .Select(key => items1Dictionary[key])
                .ToArray();

            var item2Values = items2Dictionary.Keys
                .Except(intersectingKeys)
                .Select(key => items2Dictionary[key])
                .ToArray();

            return (intersectingValues, item1Values, item2Values);
        }

        public static IDictionary<T0, KeyValuePair<T1, T2>> Merge<T0, T1, T2>(this IEnumerable<T1> items1,
                IEnumerable<T2> items2,
                Func<T1, T0> propertySelection1,
                Func<T2, T0> propertySelection2,
            Func<T0, T0, bool> predicateComparer,
            Func<T0, int> hash = default(Func<T0, int>))
        {
            return items1.Merge(items2,
                propertySelection1, propertySelection2,
                (dictionaryKvp, item1UnmatchedDiscard, items2UnmatchedDiscard) => dictionaryKvp,
                predicateComparer, hash);
        }

        public interface IMergeResult<TMerge>
        {
            bool IsEmpty { get; }
            TMerge Value { get; }
        }

        public static IEnumerable<TMerge> Merge<T1, T2, TMerge>(this IEnumerable<T1> items1,
                IEnumerable<T2> items2,
                Func<IEnumerator<T1>, IEnumerator<T2>, 
                    Func<TMerge, IMergeResult<TMerge>>, Func<IMergeResult<TMerge>>,
                    IMergeResult<TMerge>> merger)
        {
            var enumerator1 = items1.GetEnumerator();
            var enumerator2 = items2.GetEnumerator();

            while(true)
            {
                var result = merger(enumerator1, enumerator2,
                    (value) => new MergeResult<TMerge>(value),
                    () => new EmptyMergeResult<TMerge>());
                if (result.IsEmpty)
                    yield break;
                yield return result.Value;
            }
        }

        private class MergeResult<TMerge> : IMergeResult<TMerge>
        {
            private TMerge value;

            public MergeResult(TMerge value)
            {
                this.value = value;
            }

            public bool IsEmpty => false;

            public TMerge Value => value;
        }

        private class EmptyMergeResult<TMerge> : IMergeResult<TMerge>
        {
            public bool IsEmpty => true;

            public TMerge Value => throw new NotImplementedException();
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
                items1
                    .NullToEmpty()
                    .Select(item => propertySelection1(item).PairWithValue(item))
                    .ToDictionary();
            var item2Lookup =
                items2.NullToEmpty().Select(item => propertySelection2(item).PairWithValue(item))
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

        public static IEnumerable<T1> Wheres<T1, T2>(this IEnumerable<T1> items1,
                IEnumerable<T2> items2,
                Func<T1, T2, bool> predicateComparer)
        {
            var items2Lookups = items2.ToArray();
            return items1
                .Where(
                    item1 => items2Lookups
                        .Any(
                            (item2) =>
                            {
                                return predicateComparer(item1, item2);
                            }));
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

        public static bool AnyNullSafe<TItem>(this IEnumerable<TItem> items)
        {
            return items.NullToEmpty().Any();
        }

        public static bool None<TItem>(this IEnumerable<TItem> items)
        {
            return !items.AnyNullSafe();
        }

        public static bool TryGetValueNullSafe<TKey, TValue>(this IDictionary<TKey, TValue> items, TKey key, out TValue value)
        {
            if (!items.IsDefaultOrNull())
                return items.TryGetValue(key, out value);

            value = default;
            return false;
        }

        public static bool ContainsNullSafe<TItem>(this IEnumerable<TItem> items, TItem item)
        {
            if (items == null)
                return false;
            return items.Contains(item);
        }

        public static bool IsSingle<TItem>(this IEnumerable<TItem> items)
        {
            if (items.IsDefaultOrNull())
                return false; // Is Null

            var enumerator = items.GetEnumerator();

            if (!enumerator.MoveNext())
                return false; // Is Empty

            if (enumerator.MoveNext())
                return false; // Has two or more

            return true; // Is Single
        }

        public static TResult Single<TItem, TResult>(this IEnumerable<TItem> items,
            Func<TItem, TResult> onSingle,
            Func<TResult> onNoneOrMultiple)
        {
            if (items.IsDefaultOrNull())
                return onNoneOrMultiple(); // Is Null

            var enumerator = items.GetEnumerator();

            if (!enumerator.MoveNext())
                return onNoneOrMultiple(); // Is Empty

            var item = enumerator.Current;

            if (enumerator.MoveNext())
                return onNoneOrMultiple(); // Has two or more

            return onSingle(item); // Is Single
        }

        public static bool TrySingle<TItem>(this IEnumerable<TItem> items, out TItem item)
        {
            using (var enumerator = items.GetEnumerator())
            {
                if (!enumerator.MoveNext())
                {
                    item = default;
                    return false;
                }
                item = enumerator.Current;
                if (enumerator.MoveNext())
                    return false;
                return true;
            }
        }

        public static IEnumerable<TAs> IsAs<TItem, TAs>(this IEnumerable<TItem> items)
        {
            return items
                .Where(item => item is TAs)
                .Cast<TAs>();
        }

        public static IEnumerable<TAs> IsAs<TItem, TAs>(this IEnumerable<TItem> items,
            Func<TItem, TAs> tCastAs)
        {
            return items
                .Where(item => item is TAs)
                .Select(tCastAs);
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

        public static TResult MinOrEmpty<TItem, TCriteria, TResult>(this IEnumerable<TItem> items,
            Func<TItem, TCriteria> sortCriteria,
            Func<TItem, TCriteria, TResult> success,
            Func<TResult> emptyItems)
            where TCriteria : IComparable
        {
            var enumerator = items.NullToEmpty().GetEnumerator();
            if (!enumerator.MoveNext())
                return emptyItems();

            var best = enumerator.Current;
            var bestCriteria = sortCriteria(best);

            while (enumerator.MoveNext())
            {
                var challenger = enumerator.Current;
                var challengerCriteria = sortCriteria(challenger);
                if (challengerCriteria.CompareTo(bestCriteria) < 0)
                {
                    best = challenger;
                    bestCriteria = challengerCriteria;
                }
            }
            return success(best, bestCriteria);
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

        public static TResult MaxOrEmpty<TItem, TCriteria, TResult>(this IEnumerable<TItem> items,
            Func<TItem, TCriteria> sortCriteria,
            Func<TItem, TCriteria, TResult> success,
            Func<TResult> emptyItems)
            where TCriteria : IComparable
        {
            var enumerator = items.NullToEmpty().GetEnumerator();
            if (!enumerator.MoveNext())
                return emptyItems();

            var best = enumerator.Current;
            var bestCriteria = sortCriteria(best);

            while (enumerator.MoveNext())
            {
                var challenger = enumerator.Current;
                var challengerCriteria = sortCriteria(challenger);
                if (challengerCriteria.CompareTo(bestCriteria) > 0)
                {
                    best = challenger;
                    bestCriteria = challengerCriteria;
                }
            }
            return success(best, bestCriteria);
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
            while (true)
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
            foreach (var item in items.NullToEmpty())
            {
                if (doesContain(item))
                    return true;
            }
            return false;
        }

        public static bool Contains(this IEnumerable<string> items, string match, StringComparison stringComparison)
        {
            foreach (var item in items.NullToEmpty())
            {
                if (match.Equals(item, comparisonType: stringComparison))
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


#if NET5_0

        public delegate bool TryPredicate<TItem, TOut>(TItem item, out TOut result);
        public static IEnumerable<(TItem item, TOut @out)> TryWhere<TItem, TOut>(this IEnumerable<TItem> items,
            TryPredicate<TItem, TOut> tryPredicate)
        {
            return items
                .Select(
                    (item) =>
                    {
                        var success = tryPredicate(item, out TOut @out);
                        return (success, item, @out);
                    })
                .Where(item => item.success)
                .Select(item => (item.item, item.@out));
        }

        public delegate bool TryPredicate2<TItem, TOut1, TOut2>(TItem item,
            out TOut1 result1, out TOut2 result2);
        public static IEnumerable<(TItem item, TOut1 @out1, TOut2 @out2)> TryWhere<TItem, TOut1, TOut2>(this IEnumerable<TItem> items,
            TryPredicate2<TItem, TOut1, TOut2> tryPredicate)
        {
            return items
                .Select(
                    (item) =>
                    {
                        var success = tryPredicate(item, out TOut1 @out1, out TOut2 @out2);
                        return (success, item, @out1, @out2);
                    })
                .Where(item => item.success)
                .Select(item => (item.item, item.@out1, item.@out2));
        }

        public delegate bool TryPredicate3<TItem, TOut1, TOut2, TOut3>(TItem item,
            out TOut1 result1, out TOut2 result2, out TOut3 result3);
        public static IEnumerable<(TItem item, TOut1 @out1, TOut2 @out2, TOut3 @out3)> TryWhere<TItem, TOut1, TOut2, TOut3>(this IEnumerable<TItem> items,
            TryPredicate3<TItem, TOut1, TOut2, TOut3> tryPredicate)
        {
            return items
                .Select(
                    (item) =>
                    {
                        var success = tryPredicate(item, out TOut1 @out1, out TOut2 @out2, out TOut3 @out3);
                        return (success, item, @out1, @out2, @out3);
                    })
                .Where(item => item.success)
                .Select(item => (item.item, item.@out1, item.@out2, item.@out3));
        }

        public static IEnumerable<T3> SelectWhere<T1, T2, T3>(this IEnumerable<(T1, T2)> items,
            Func<(T1, T2), (bool, T3)> isWhere)
        {
            foreach (var item in items)
            {
                var (isSelected, r) = isWhere(item);
                if (isSelected)
                    yield return r;
            }
        }

        public static IEnumerable<T> SelectWhere<T>(this IEnumerable<(bool, T)> items)
        {
            return items.SelectWhere(
                item => (item.Item1, item.Item2));
        }

#endif

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

        public static TResult First<TITem, TResult>(this IEnumerable<TITem> items,
            Func<TITem, Func<TResult>, TResult> next,
            Func<TResult> onNotFound)
        {
            var enumerator = items.GetEnumerator();
            return enumerator.First(next, onNotFound);
        }

        private static TResult First<TITem, TResult>(this IEnumerator<TITem> items,
            Func<TITem, Func<TResult>, TResult> next,
            Func<TResult> onNotFound)
        {
            if (!items.MoveNext())
                return onNotFound();

            return next(items.Current, () => items.First(next, onNotFound));
        }

        public static TResult LastOrEmpty<TITem, TResult>(this IEnumerable<TITem> items,
            Func<TITem, TResult> onLast,
            Func<TResult> onEmpty)
        {
            var enumerator = items.GetEnumerator();
            if (!enumerator.MoveNext())
                return onEmpty();
            return onLast(items.Last());
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

        public static IEnumerable<TResult> SelectBySegment<TItem, TResult>(this IEnumerable<TItem> items,
             Func<TItem, TResult> single,
             Func<TItem, TItem, TResult> first,
             Func<TItem, TItem, TItem, TResult> middle,
             Func<TItem, TItem, TResult> last)
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

            while (iter.MoveNext())
            {
                yield return middle(lastValue, current, iter.Current);
                lastValue = current;
                current = iter.Current;
            }
            yield return last(lastValue, current);
        }

#if NET5_0

        public static IEnumerable<T2> SelectWhere<T1, T2>(this IEnumerable<(T1, T2)> items,
            Func<(T1, T2), bool> isWhere)
        {
            foreach (var item in items)
            {
                if (isWhere(item))
                    yield return item.Item2;
            }
        }

        public static IEnumerable<(T4, T5)> SelectWhere<T1, T2, T3, T4, T5>(this IEnumerable<(T1, T2, T3)> items,
            Func<(T1, T2, T3), (bool, T4, T5)> isWhere)
        {
            foreach (var item in items)
            {
                var (isSelected, r1, r2) = isWhere(item);
                if (isSelected)
                    yield return (r1, r2);
            }
        }

#endif

        public delegate TResult SelectWithDelegate<TWith, TItem, TResult>(TWith previous, TItem current, out TWith next);
        public static IEnumerable<TResult> SelectWith<TWith, TItem, TResult>(this IEnumerable<TItem> items,
            TWith seed, SelectWithDelegate<TWith, TItem, TResult> callback)
        {
            var carry = seed;
            foreach (var item in items)
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
            while (peeked || enumerator.MoveNext())
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

            public bool HasValue { get; private set; }

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
            while (itemsAvailable.Any())
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
            foreach (var item in items)
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


        /// <summary>
        /// Sort items by removing a group of items at a time. 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="items"></param>
        /// <param name="sorter"></param>
        /// <param name="comparison"></param>
        /// <returns></returns>
        public static IEnumerable<T> Sort<T>(this IEnumerable<T> items,
            Func<IEnumerable<T>, IEnumerable<T>, IEnumerable<T>> sorter,
            Func<T, T, bool> comparison)
        {
            var sorted = new T[] { };
            var unsorted = items.ToArray();
            while (true)
            {
                var newSorted = sorter(unsorted, sorted).ToArray();
                var newUnsorted = unsorted
                    .Where(i1 => !newSorted.Where(i2 => comparison(i1, i2)).Any())
                    .ToArray();

                // sorting is done
                if (!newUnsorted.Any())
                    return newSorted;

                // sorting has stalled, TODO: Message?
                if (newUnsorted.Length == unsorted.Length)
                    return newSorted;

                // Prepare to sort again
                unsorted = newUnsorted;
                sorted = newSorted;
            }
        }

#if NET5_0

        public static IEnumerable<TMatch> Match<T1, T2, TMatch, TKey>(this IEnumerable<T1> items1,
                IEnumerable<T2> items2,
            Func<T1, T2, TMatch> matcher,
            Func<TMatch, TKey> valueSelector,
            Func<T1, TMatch> unmatchedValueSelection = default)
        {
            return items1
                .Select(
                    item1 =>
                    {
                        var item1c = item1;
                        return items2
                            .Select(
                                item2 =>
                                {
                                    var value = matcher(item1c, item2);
                                    return value;
                                })
                            .OrderBy(value => valueSelector(value))
                            .First(
                                (tuple, next) => (true, (TMatch)tuple),
                                () =>
                                {
                                    if (unmatchedValueSelection.IsDefaultOrNull())
                                        return (false, default(TMatch));
                                    var emptyMatch = unmatchedValueSelection(item1c);
                                    return (true, emptyMatch);
                                });
                    })
                .Where(tuple => tuple.Item1)
                .Select(tuple => tuple.Item2);
        }

        public static IEnumerable<(T1, T2)> CollateSimple<T1, T2>(this IEnumerable<T1> items1,
                IEnumerable<T2> items2)
        {
            var iterator1 = items1.NullToEmpty().GetEnumerator();
            var iterator2 = items2.NullToEmpty().GetEnumerator();
            while (iterator1.MoveNext())
            {
                if (!iterator2.MoveNext())
                    yield break;
                yield return (iterator1.Current, iterator2.Current);
            }
        }

        public struct Collated<T1, T2, TKey>
        {
            public TKey value;
            public T1 item1;
            public T2 item2;
        }

        public static IEnumerable<Collated<T1, T2, TKey>> Collate<T1, T2, TKey>(this IEnumerable<T1> items1,
                IEnumerable<T2> items2,
            Func<T1, T2, TKey> collater)
        {
            return items1.Collate(items2,
                (item1, item2) =>
                {
                    var value = collater(item1, item2);
                    return new Collated<T1, T2, TKey>
                    {
                        item1 = item1,
                        item2 = item2,
                        value = value,
                    };
                },
                collation => collation.value);
        }

        public static IEnumerable<TCollate> Collate<T1, T2, TCollate, TKey>(this IEnumerable<T1> items1,
                IEnumerable<T2> items2,
            Func<T1, T2, TCollate> collater,
            Func<TCollate, TKey> valueSelector)
        {
            var items1Knockout = items1.ToList();
            var items2Knockout = items2.ToList();
            return items1
                .SelectMany(
                    item1 => items2
                        .Select(
                            item2 =>
                            {
                                var value = collater(item1, item2);
                                return (value, item1, item2);
                            }))
                .OrderBy(tuple => valueSelector(tuple.value))
                .Where(
                    collation =>
                    {
                        if (!items1Knockout.Contains(collation.item1))
                            return false;
                        if (!items2Knockout.Contains(collation.item2))
                            return false;
                        if (!items1Knockout.Remove(collation.item1))
                            throw new Exception();
                        if (!items2Knockout.Remove(collation.item2))
                            throw new Exception();
                        return true;
                    })
                .Select(tuple => tuple.value);
        }
#endif

        public static IEnumerable<T> Compress<T>(this IEnumerable<T> items, Func<T, T, T[]> compressor)
        {
            var iterator = items.GetEnumerator();
            if (!iterator.MoveNext())
                yield break;
            var current = iterator.Current;

            while (iterator.MoveNext())
            {
                var compressedResults = compressor(current, iterator.Current);
                if (!compressedResults.Any())
                    continue;

                current = compressedResults.Last();
                foreach (var item in compressedResults.Take(compressedResults.Length - 1))
                {
                    yield return item;
                }
            }
            yield return current;
        }

        public static IEnumerable<KeyValuePair<T1, T2>> Combine<T1, T2>(this IEnumerable<T1> items1, IEnumerable<T2> combineWith)
        {
            foreach (var item1 in items1)
                foreach (var item2 in combineWith)
                    yield return item1.PairWithValue(item2);
        }

        public static T[][] Combinations<T>(this IEnumerable<T> items, bool fullSetsOnly = false)
        {
            var itemsArray = items.ToArray();

            if (itemsArray.Length == 0)
                return new T[][] { };

            if (itemsArray.Length == 1)
                return new T[][] { itemsArray };

            var item = itemsArray[0];
            var remainder = items.Skip(1).ToArray();
            var combinationsRemainder = remainder
                .Combinations(fullSetsOnly: fullSetsOnly);
            if (fullSetsOnly)
            {
                var combinationsFullSets = combinationsRemainder
                    .Select(co => co.Append(item).ToArray())
                    .ToArray();
                return combinationsFullSets;
            }
            var combinations = combinationsRemainder
                .Select(co => co.Append(item).ToArray())
                .Concat(combinationsRemainder)
                .Append(new[] { item })
                .ToArray();
            return combinations;
        }

#if NET5_0
        public static IEnumerable<(T1, T2)[]> Combinations<T1, T2>(this IEnumerable<T1> items1, IEnumerable<T2> items2)
        {
            if (items1.IsDefaultNullOrEmpty())
                yield break;
            if (items2.IsDefaultNullOrEmpty())
                yield break;

            var items1Array = items1.ToArray();
            var items2Array = items2.ToArray();

            //if(items1Array.Length > items2Array.Length)
            //{
            //    var leftOvers = items2Array
            //        .Skip(items1Array.Length)
            //        .ToArray();
            //    items1Array
            //        .Permutations()
            //        .Select(
            //            permutation =>
            //            {
            //                permutation.Select()
            //            })
            //}

            if (items1Array.Length == 1)
            {
                var item1 = items1Array.First();
                foreach (var item2 in items2Array)
                    yield return (item1, item2).AsArray();
                yield break;
            }

            if (items2Array.Length == 1)
            {
                var item2 = items2Array.First();
                foreach (var item1 in items1Array)
                    yield return (item1, item2).AsArray();
                yield break;
            }

            foreach (var item1 in items1Array)
            {
                var items1Remainder = items1Array
                    .Exclude(item1, limit: 1)
                    .ToArray();
                foreach (var item2 in items2Array)
                {
                    var items2Remainder = items2Array
                        .Exclude(item2, limit: 1)
                        .ToArray();
                    var subCombinations = items1Remainder
                        .Combinations(items2Remainder)
                        .ToArray();
                    foreach (var subCombination in subCombinations)
                        yield return subCombination
                                .Append((item1, item2))
                                .ToArray();
                }
            }
        }

        public static IEnumerable<(T, T)> Pairs<T>(this IEnumerable<T> items, bool sameItemPairs = false)
        {
            var itemsArray = items.ToArray();

            if(sameItemPairs)
            {
                foreach (var i1 in Enumerable.Range(0, itemsArray.Length))
                    foreach (var i2 in Enumerable.Range(0, itemsArray.Length))
                        yield return (itemsArray[i1], itemsArray[i2]);
                yield break;
            }

            if (itemsArray.Length < 2)
                yield break;

            var index1 = 0;
            while (index1 < itemsArray.Length - 1)
            {
                var index2 = index1 + 1;
                while (index2 < itemsArray.Length)
                {
                    yield return (itemsArray[index1], itemsArray[index2]);
                    index2++;
                }
                index1++;
            }
        }
#endif

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

        public static T[] AsCopy<T>(this IEnumerable<T> items)
        {
            var itemsArray = items.ToArray();
            var copy = new T[itemsArray.Length];
            Array.Copy(itemsArray, copy, itemsArray.Length);
            return copy;
        }

        public static bool ContainsEqual<TItem>(this IEnumerable<TItem> items1, IEnumerable<TItem> items2)
        {
            var items1Arr = items1.ToArray();
            var items2Arr = items2.ToArray();
            if (items1Arr.Count() != items2Arr.Count())
                return false;
            return items1Arr.All(item => items2Arr.Contains(item));
        }

        public static bool SequenceEqual(this IEnumerable<string> items1, IEnumerable<string> items2, StringComparison stringComparison)
        {
            var items1Arr = items1.ToArray();
            var items2Arr = items2.ToArray();
            if (items1Arr.Length != items2Arr.Length)
                return false;
            return items1Arr
                .CollateSimple(items2Arr)
                .All(tpl => tpl.Item1.Equals(tpl.Item2, stringComparison));
        }

        //public static bool SequenceEqual<TItem>(this IEnumerable<TItem> items1, IEnumerable<TItem> items2)
        //    where TItem : class
        //{
        //    return items1.Zip(items2, (i1, i2) => i1 == i2).All(b => b);
        //}
    }
}

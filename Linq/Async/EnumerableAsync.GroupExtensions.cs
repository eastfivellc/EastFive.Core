using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EastFive.Linq.Async
{
    public static partial class EnumerableAsync
    {

        private class GroupingAsync<TKey, TElement> : IGroupingAsync<TKey, TElement>
        {
            public TKey Key { get; private set; }

            private IEnumerableAsync<TElement> enumerable;

            public IEnumeratorAsync<TElement> GetEnumerator()
            {
                return enumerable.GetEnumerator();
            }

            public GroupingAsync(TKey key, IEnumerableAsync<TElement> enumerable, Func<TElement, TKey> keySelector)
            {
                this.Key = key;
                this.enumerable = enumerable.Where(
                    item => key.Equals(keySelector(item)));
            }
        }

        public static IEnumerableAsync<IGroupingAsync<TKey, TSource>> GroupBy<TSource, TKey>(this IEnumerableAsync<TSource> enumerable,
            Func<TSource, TKey> keySelector)
        {
            var accumulation = new Dictionary<TKey, GroupingAsync<TKey, TSource>>();
            return new DelegateEnumerableAsync<IGroupingAsync<TKey, TSource>, TSource>(enumerable,
                async (enumeratorAsync, enumeratorDestination, moved, ended) =>
                {
                    if (!await enumeratorAsync.MoveNextAsync())
                        return ended();
                    var current = enumeratorAsync.Current;
                    var key = keySelector(current);
                    while (accumulation.ContainsKey(key))
                    {
                        if (!await enumeratorAsync.MoveNextAsync())
                            return ended();
                        current = enumeratorAsync.Current;
                    }
                    var grouping = new GroupingAsync<TKey, TSource>(key, enumerable, keySelector); // TODO: Pass enumerable that beings at this point since there are clearly no elements matching the key before this point.
                    accumulation.Add(key, grouping); 
                    return moved(grouping);
                });
        }
    }
}

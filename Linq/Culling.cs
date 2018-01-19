using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EastFive.Linq
{
    public static class Culling
    {
        public static IEnumerable<TItem> If<TItem>(this IEnumerable<TItem> items,
            bool condition,
            Func<IEnumerable<TItem>, IEnumerable<TItem>> ifTrue)
        {
            if (condition)
                return ifTrue(items);
            return items;
        }

        public static IEnumerable<TItem> If<TItem>(this IEnumerable<TItem> items,
            bool condition,
            Func<IEnumerable<TItem>, IEnumerable<TItem>> ifTrue,
            Func<IEnumerable<TItem>, IEnumerable<TItem>> ifNotTrue)
        {
            if (condition)
                return ifTrue(items);
            return ifNotTrue(items);
        }

        public static IEnumerable<TItem> IfWhere<TItem>(this IEnumerable<TItem> items,
            bool condition,
            Func<TItem, bool> predicate)
        {
            return items.If(condition, 
                itemsIfTrue => itemsIfTrue.Where(predicate));
        }
    }
}

using System.Collections.Generic;

namespace BlackBarLabs.Core.Collections
{
    public static class ListExtensions
    {
        public static void AddIfNotExisting<TValue>(this IList<TValue> list, TValue value)
        {
            if (!list.Contains(value))
            {
                list.Add(value);
            }
        }
    }
}

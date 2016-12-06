﻿using System;
using System.Collections.Generic;

namespace BlackBarLabs.Core.Collections
{
    public static class ListExtensions
    {
        public static IEnumerable<TValue> AddIfNotExisting<TValue>(this IEnumerable<TValue> items, TValue value)
            where TValue : IComparable
        {
            var found = false;
            foreach(var item in items)
            {
                if (!found && item.CompareTo(value) == 0)
                    found = true;
                yield return item;
            }
            if (!found)
                yield return value;
        }
    }
}

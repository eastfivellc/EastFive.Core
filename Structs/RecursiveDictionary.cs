using BlackBarLabs.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlackBarLabs.Collections.Generic
{
    public class RecursiveDictionary<TKey> : Dictionary<TKey, RecursiveDictionary<TKey>>
    {
    }

    public static class RecursiveDictionaryExtensions
    {

        public static IEnumerable<T> Flatten<T>(this RecursiveDictionary<T> dictionary)
        {
            return dictionary
                .SelectMany(
                    kvp =>
                    {
                        if (default(RecursiveDictionary<T>) == kvp.Value)
                            return new T[] { kvp.Key };
                        return dictionary.Flatten().Append(kvp.Key);
                    });
        }
    }

}

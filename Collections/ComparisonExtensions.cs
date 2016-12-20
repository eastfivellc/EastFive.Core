using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlackBarLabs.Collections.Generic
{
    public static class ComparisonExtensions
    {
        private class Comparer<T> : System.Collections.Generic.Comparer<T>
        {
            public Func<T, T, int> compare;

            public override int Compare(T x, T y)
            {
                return compare(x, y);
            }
        }

        private class EqualityComparer<T> : System.Collections.Generic.EqualityComparer<T>
        {
            public Func<T, T, int> compare;
            
            public override bool Equals(T x, T y)
            {
                return compare(x, y) == 0;
            }

            public override int GetHashCode(T obj)
            {
                return obj.GetHashCode();
            }
        }

        public static IComparer<T> ToComparer<T>(this Func<T, T, int> compare)
        {
            var comparer = new Comparer<T>();
            comparer.compare = compare;
            return comparer;
        }

        public static IEqualityComparer<T> ToEqualityComparer<T>(this Func<T, T, int> compare)
        {
            var comparer = new EqualityComparer<T>();
            comparer.compare = compare;
            return comparer;
        }
    }
}

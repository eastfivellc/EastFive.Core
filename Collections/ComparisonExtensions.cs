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

        private class EqualityComparerPredicate<T> : System.Collections.Generic.EqualityComparer<T>
        {
            public Func<T, T, bool> areEqual;
            public Func<T, int> hash;

            public override bool Equals(T x, T y)
            {
                return areEqual(x, y);
            }

            public override int GetHashCode(T obj)
            {
                return hash(obj);
            }
        }

        public static IComparer<T> ToComparer<T>(this Func<T, T, int> compare)
        {
            var comparer = new Comparer<T>();
            comparer.compare = compare;
            return comparer;
        }

        public static IEqualityComparer<T> ToEqualityComparer<T>(this Func<T, T, int> compare,
            Func<T, int> hash = default(Func<T, int>))
        {
            Func<T, T, bool> areEqual = (v1, v2) => compare(v1, v2) == 0;
            return areEqual.ToEqualityComparer(hash);
        }

        public static IEqualityComparer<T> ToEqualityComparer<T>(this Func<T, T, bool> areEqual,
            Func<T, int> hash = default(Func<T, int>))
        {
            var comparer = new EqualityComparerPredicate<T>();
            comparer.areEqual = areEqual;
            comparer.hash = (default(Func<T, int>) == hash) ?
                (obj) => obj.GetHashCode()
                :
                hash;
            
            return comparer;
        }
    }
}

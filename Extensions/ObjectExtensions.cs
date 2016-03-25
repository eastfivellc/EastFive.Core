using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlackBarLabs.Core.Extensions // Make user force extensions because this affects _every_ object
{
    public static class ObjectExtensions
    {
        public static T OrIfDefault<T>(this T value, T alternative)
            where T : IComparable
        {
            if (EqualityComparer<T>.Default.Equals(value, default(T)))
                return alternative;
            return value;
        }
    }
}

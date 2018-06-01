using BlackBarLabs.Extensions;
using EastFive.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EastFive.Linq
{
    public static class MathExtensions
    {
        public static IEnumerable<double> Normalize(this IEnumerable<double> items,
            double baseWeight = 1.0)
        {
            var total = items.Sum();
            var weight = baseWeight / total;
            foreach (var item in items)
            {
                yield return item * weight;
            }
        }
        
    }
}

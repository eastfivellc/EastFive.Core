using BlackBarLabs.Collections.Generic;
using BlackBarLabs.Linq;
using EastFive.Collections.Generic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BlackBarLabs.Extensions;
using EastFive.Linq;

namespace EastFive
{
    public static class NullableExtensions
    {
        public static TResult GetValueOrDefault<T, TResult>(this Nullable<T> valueMaybe,
            Func<T, TResult> onValue,
            TResult defaultValue)
            where T : struct
        {
            if (!valueMaybe.HasValue)
                return defaultValue;
            var result = onValue(valueMaybe.Value);
            return result;
        }
        
    }
}

using EastFive.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EastFive
{
    public static class RefExtensions
    {
        public static bool HasValueNotNull<T>(this IRefOptional<T> refOptional) where T : struct
        {
            if (refOptional.IsDefaultOrNull())
                return false;
            return refOptional.HasValue;
        }
    }
}

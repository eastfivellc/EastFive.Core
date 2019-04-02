using EastFive.Extensions;
using EastFive.Linq.Async;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EastFive
{
    public static class RefExtensions
    {
        public static IRef<T> AsRef<T>(this Guid id)
            where T : struct
        {
            return new Ref<T>(id);
        }

        public static IRefs<T> AsRefs<T>(this IEnumerable<Guid> ids)
            where T : struct, IReferenceable
        {
            return new Refs<T>(ids.ToArray());
        }

        public static IRefs<T> AsRefs<T>(this IEnumerableAsync<T> ids)
            where T : struct, IReferenceable
        {
            return new Refs<T>(ids);
        }

        public static IRefOptional<T> Optional<T>(this IRef<T> baseRef)
            where T: struct
        {
            return new RefOptional<T>(baseRef);
        }

        public static bool HasValueNotNull<T>(this IRefOptional<T> refOptional) where T : struct
        {
            if (refOptional.IsDefaultOrNull())
                return false;
            return refOptional.HasValue;
        }
    }
}

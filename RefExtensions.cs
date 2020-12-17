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
            where T : IReferenceable
        {
            return new Ref<T>(id);
        }

        public static IRef<TType> AsRef<TType>(this TType type)
            where TType : IReferenceable
        {
            return new EastFive.Ref<TType>(type.id);
        }

        public static IRefs<T> AsRefs<T>(this IEnumerable<Guid> ids)
            where T : IReferenceable
        {
            return new Refs<T>(ids.ToArray());
        }

        public static IRefs<T> Refs<T>(this IEnumerable<IRef<T>> refs)
            where T : IReferenceable
        {
            return refs.Select(r => r.id).AsRefs<T>();
        }

        public static IRefs<T> AsRefs<T>(this IEnumerable<T> refs)
            where T : IReferenceable
        {
            return refs.Select(r => r.id).AsRefs<T>();
        }

        public static async Task<IRefs<T>> AsRefsAsync<T>(this IEnumerableAsync<Guid> ids)
            where T : IReferenceable
        {
            var idsArray = await ids.ToArrayAsync();
            return new Refs<T>(idsArray);
        }

        public static Task<IRefs<T>> AsRefsAsync<T>(this IEnumerableAsync<IRef<T>> refs)
            where T : IReferenceable
        {
            return refs.Select(r => r.id).AsRefsAsync<T>();
        }

        public static IRefOptional<T> Optional<T>(this IRef<T> baseRef)
            where T: IReferenceable
        {
            return new RefOptional<T>(baseRef);
        }

        public static IRefOptional<T> AsRefOptional<T>(this Guid? baseRef)
            where T : IReferenceable
        {
            if (!baseRef.HasValue)
                return new RefOptional<T>();

            return baseRef.Value.AsRef<T>().Optional();
        }

        public static IRefOptional<TType> AsRefOptional<TType>(this Guid guid)
            where TType : IReferenceable
        {
            return guid.AsOptional().AsRefOptional<TType>();
        }

        public static IRef<TCast> CastRef<TCast>(this IReferenceable sourceRef)
            where TCast : IReferenceable
        {
            return sourceRef.id.AsRef<TCast>();
        }

        public static bool IsPopulated<T>(this IRefOptional<T> refOptional)
            where T : IReferenceable
        {
            return refOptional.HasValue && !refOptional.Ref.id.IsDefault();
        }

        public static bool HasValueNotNull<T>(this IRefOptional<T> refOptional) 
            where T : IReferenceable
        {
            if (refOptional.IsDefaultOrNull())
                return false;
            return refOptional.HasValue;
        }
    }
}

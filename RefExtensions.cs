using EastFive.Extensions;
using EastFive.Linq;
using EastFive.Linq.Async;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.PortableExecutable;
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
            if (sourceRef == null)
                return null;
            return sourceRef.id.AsRef<TCast>();
        }

        public static bool TryParseRef(this string refString, Type type, out object parsedRefObj)
        {
            if (type.IsSubClassOfGeneric(typeof(IReferenceable)))
            {
                if (type.IsSubClassOfGeneric(typeof(IRef<>)))
                {
                    var refType = typeof(Ref<>).MakeGenericType(type.GenericTypeArguments);
                    if (Guid.TryParse(refString, out Guid id))
                    {
                        parsedRefObj = Activator.CreateInstance(refType, id);
                        return true;
                    }
                    parsedRefObj = refType.GetDefault();
                    return false;
                }
            }

            if (type.IsSubClassOfGeneric(typeof(IReferenceableOptional)))
            {
                if (type.IsSubClassOfGeneric(typeof(IRefOptional<>)))
                {
                    var refType = typeof(RefOptional<>).MakeGenericType(type.GenericTypeArguments);
                    if (Guid.TryParse(refString, out Guid id))
                    {
                        parsedRefObj = Activator.CreateInstance(refType, id);
                        return true;
                    }
                    parsedRefObj = RefOptionalHelper.CreateEmpty(type.GenericTypeArguments.First());
                    return true;
                }
            }

            if (type.IsSubClassOfGeneric(typeof(IReferences)))
            {
                if (type.IsSubClassOfGeneric(typeof(IRefs<>)))
                {
                    var ids = GetGuids();
                    var refType = typeof(Refs<>).MakeGenericType(type.GenericTypeArguments);
                    parsedRefObj = Activator.CreateInstance(refType, ids);
                    return true;
                }
            }

            parsedRefObj = type.GetDefault();
            return false;

            Guid[] GetGuids()
            {
                if (refString.IsNullOrWhiteSpace())
                    return new Guid[] { };

                return refString
                    .Split(',')
                    .TrySelect(
                        (string guidStr, out Guid guid) =>
                        {
                            return Guid.TryParse(guidStr, out guid);
                        })
                    .ToArray();
            }
        }

        public static bool TryCastRef(this Guid refGuid, Type type, out object parsedRefObj)
        {
            if (type.IsSubClassOfGeneric(typeof(IReferenceable)))
            {
                if (type.IsSubClassOfGeneric(typeof(IRef<>)))
                {
                    var refType = typeof(Ref<>).MakeGenericType(type.GenericTypeArguments);
                    parsedRefObj = Activator.CreateInstance(refType, refGuid);
                    return true;
                }
            }

            if (type.IsSubClassOfGeneric(typeof(IReferenceableOptional)))
            {
                if (type.IsSubClassOfGeneric(typeof(IRefOptional<>)))
                {
                    var refType = typeof(RefOptional<>).MakeGenericType(type.GenericTypeArguments);
                    parsedRefObj = Activator.CreateInstance(refType, refGuid);
                    return true;
                }
            }

            parsedRefObj = type.GetDefault();
            return false;

        }

        public static bool HasValueNotNull<T>(this IRefOptional<T> refOptional) 
            where T : IReferenceable
        {
            if (refOptional.IsDefaultOrNull())
                return false;
            return refOptional.HasValue && !refOptional.Ref.id.IsDefault();
        }

        public static Guid? GetIdMaybeNullSafe<T>(this IRefOptional<T> refOptional)
            where T : IReferenceable
        {
            if (refOptional.IsDefaultOrNull())
                return default;
            return refOptional.id;
        }

        public static bool EqualsRef<T>(this IRef<T> refValue1, IRef<T> refValue2)
            where T : IReferenceable
        {
            return refValue1.id == refValue2.id;
        }

        public static bool EqualsRef<T>(this IRefOptional<T> refOptional, IRef<T> refValue)
            where T : IReferenceable => refValue.EqualsRef(refOptional);

        public static bool EqualsRef<T>(this IRef<T> refValue,  IRefOptional<T> refOptional)
            where T : IReferenceable
        {
            if (refOptional.IsDefaultOrNull())
                return false;
            if (!refOptional.HasValue)
                return false;
            return refValue.EqualsRef(refOptional.Ref);
        }

        public static bool EqualsRef<T>(this IRefOptional<T> refOptional1, IRefOptional<T> refOptional2)
            where T : IReferenceable
        {
            if (refOptional1.HasValueNotNull())
            {
                var refValue = refOptional1.Ref;
                return refValue.EqualsRef(refOptional2);
            }
            return !refOptional2.HasValueNotNull();
        }
    }
}

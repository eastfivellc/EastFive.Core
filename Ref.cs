using BlackBarLabs.Extensions;
using EastFive.Extensions;
using EastFive.Linq.Async;
using EastFive.Security;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace EastFive
{
    #region IRef(Obj)

    public interface IReferenceable
    {
        Guid id { get; }
    }

    public interface IRef
    {
        Guid id { get; }
    }

    public interface IRef<TType> : IReferenceable // TODO: , IRef
        where TType : IReferenceable
    {
        // Guid id { get; }
    }

    #endregion

    #region IRef(Obj)Optional

    public interface IReferenceableOptional
    {
        Guid? id { get; }

        bool HasValue { get; }
    }

    public interface IRefOptional<TType> : IReferenceableOptional
        where TType : IReferenceable
    {
        IRef<TType> Ref { get; }
    }

    #endregion

    #region IRef plural

    public interface IReferences
    {
        Guid[] ids { get; }
    }

    public interface IRefs<TType> : IReferences, IEnumerable<IRef<TType>>
        where  TType : IReferenceable
    {
        IRef<TType>[] refs { get; }
    }

    #endregion

    public struct Ref<TType> : IRef<TType>
        where TType : IReferenceable
    {
        public Ref(Guid id) : this()
        {
            this.id = id;
        }

        public Guid id { get; private set; }

        public static implicit operator Ref<TType>(Guid value)
        {
            return new Ref<TType>()
            {
                id = value,
            };
        }
        
        public static implicit operator Ref<TType>(TType value)
        {
            return value.IsDefault() ? 
                default(Ref<TType>) 
                :
                new Ref<TType>(value.id);
        }

        public static IRef<TType> NewRef()
        {
            return Guid.NewGuid().AsRef<TType>();
        }

        public static IRef<TType> SecureRef()
        {
            return SecureGuid.Generate().AsRef<TType>();
        }
    }

    public static class RefOptionalHelper
    {
        public static object CreateEmpty(Type type)
        {
            var emptyValue = typeof(RefOptional<>)
                .MakeGenericType(type)
                .GetMethod("Empty", BindingFlags.Static | BindingFlags.Public)
                .Invoke(null, new object[] { });
            return emptyValue;
        }
    }

    public struct RefOptional<TType> : IRefOptional<TType>
        where TType : IReferenceable
    {
        [Newtonsoft.Json.JsonIgnore]
        private IRef<TType> baseRef;

        public static IRefOptional<TType> Empty()
        {
            return new RefOptional<TType>
            {
                HasValue = false,
                baseRef = default(IRef<TType>),
            };
        }

        public RefOptional(Guid baseId)
        {
            this.HasValue = true;
            this.baseRef = new Ref<TType>(baseId);
        }

        public RefOptional(IRef<TType> baseRef)
        {
            this.HasValue = true;
            this.baseRef = baseRef;
        }

        public Guid? id
        {
            get
            {
                if (!this.HasValue)
                    return default(Guid?);
                if(this.baseRef == null)
                    return default(Guid?);
                return baseRef.id;
            }
        }

        public bool HasValue { get; private set; }

        [Newtonsoft.Json.JsonIgnore]
        public IRef<TType> Ref
        {
            get
            {
                if (!this.HasValue)
                    throw new InvalidOperationException("Attempt to de-option empty value");
                return baseRef;
            }
        }
    }

    public struct Refs<TType> : IRefs<TType>
        where TType : IReferenceable
    {
        public Refs(Guid[] ids) : this()
        {
            this.ids = ids;
        }

        public Guid[] ids { get; set; }

        public IRef<TType>[] refs
        {
            get
            {
                if (!ids.Any())
                    return new IRef<TType>[] { };
                return ids.Select(id => id.AsRef<TType>()).ToArray();
            }
        }

        IEnumerator<IRef<TType>> IEnumerable<IRef<TType>>.GetEnumerator()
        {
            return this.ids
                .Select(id => (IRef<TType>)new Ref<TType>(id))
                .GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.ids
                .Select(id => (IRef<TType>)new Ref<TType>(id))
                .GetEnumerator();
        }

        public static implicit operator Refs<TType>(Guid [] values)
        {
            return new Refs<TType>(values);
        }

        public static implicit operator Refs<TType>(TType[] values)
        {
            return values.IsDefault() ?
                new Refs<TType>(new Guid[] { })
                :
                new Refs<TType>(values.Select(v => v.id).ToArray());
        }
    }
}

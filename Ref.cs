using BlackBarLabs.Extensions;
using EastFive.Extensions;
using EastFive.Linq.Async;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
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

    public interface IRefObj<TType>
        where TType : IReferenceable
    {
        Guid id { get; }
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

    [Obsolete("Use global::EastFive.IRefOptional")]
    public interface IRefObjOptional<TType> : IReferenceableOptional
        where TType : IReferenceable
    {
        IRefObj<TType> Ref { get; }
    }

    #endregion

    #region IRef plural

    public interface IReferences
    {
        Guid[] ids { get; }
    }

    public interface IRefs<TType> : IReferences
        where  TType : IReferenceable
    {
        IRef<TType>[] refs { get; }
    }

    public interface IRefObjs<TType> : IReferences
        where TType : IReferenceable
    {
        IRefObj<TType>[] refs { get; }
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
    }

    [Obsolete("Use Ref<>")]
    public struct RefObj<TType> : IRefObj<TType>
        where TType : IReferenceable
    {
        public Guid id { get; set; }

        public RefObj(Guid id) : this()
        {
            this.id = id;
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

    [Obsolete("Use RefOptional<>")]
    public struct RefObjOptional<TType> : IRefObjOptional<TType>
        where TType : IReferenceable
    {
        private IRefObj<TType> baseRef;

        public RefObjOptional(IRefObj<TType> baseRef)
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
                return baseRef.id;
            }
        }
        public IRefObj<TType> Ref => new RefObj<TType>(this.id.Value);

        public bool HasValue { get; set; }

    }

    public struct Refs<TType> : IRefs<TType>
        where TType : IReferenceable
    {
        public Refs(IEnumerableAsync<TType> valueTask)
        {
            this.ids = default(Guid[]);
        }

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
                throw new NotImplementedException();
            }
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

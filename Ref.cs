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

    public interface IRefBase : IReferenceable
    {
        Task ResolveAsync();

        bool resolved { get; }
    }

    public interface IRef<TType> : IRefBase
        where TType : struct
    {
        TType? value { get; }
    }

    public interface IRefObj<TType> : IRefBase
        where TType : class
    {
        Func<TType> value { get; }
    }

    #endregion

    #region IRef(Obj)Optional

    public interface IReferenceableOptional
    {
        Guid? id { get; }
    }

    public interface IRefOptionalBase : IReferenceableOptional
    {
        bool HasValue { get; }
    }

    public interface IRefOptional<TType> : IRefOptionalBase
        where TType : struct
    {
        TType? value { get; }

        IRef<TType> Ref { get; }
    }

    public interface IRefObjOptional<TType> : IRefOptionalBase
        where TType : class
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
        where  TType : struct
    {
        IRef<TType>[] refs { get; }

        Linq.Async.IEnumerableAsync<TType> Values { get; }
    }

    public interface IRefObjs<TType> : IReferences
        where TType : class
    {
        IRefObj<TType>[] refs { get; }
    }

    #endregion

    public struct Ref<TType> : IRef<TType>
        where TType : struct
    {
        private Task<TType> valueTask;

        public Ref(Task<TType> valueTask)
        {
            this.id = default(Guid);
            this.value = default(TType?);
            this.valueTask = valueTask;
            this.resolved = false;
        }

        public Ref(Guid id) : this()
        {
            this.id = id;
            this.value = default(TType?);
            this.valueTask = default(Task<TType>);
            this.resolved = false;
        }

        public Guid id { get; set; }

        public TType? value { get; set; }
        
        public async Task ResolveAsync()
        {
            if (value.HasValue)
                return;
            
            this.value = await valueTask;
            this.resolved = true;
        }

        public bool resolved { get; set; }
        
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
                new Ref<TType>(value.AsTask());
        }
    }

    public struct RefObj<TType> : IRefObj<TType>
        where TType : class
    {
        public Guid id { get; set; }

        public TType value;
        public Task<TType> valueAsync;
        public bool resolved;

        public RefObj(Guid id) : this()
        {
            this.id = id;
        }

        Func<TType> IRefObj<TType>.value => throw new NotImplementedException();

        bool IRefBase.resolved => false;

        public Task ResolveAsync()
        {
            throw new NotImplementedException();
        }
    }

    public struct RefOptional<TType> : IRefOptional<TType>
        where TType : struct
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

        [Newtonsoft.Json.JsonIgnore]
        public TType? value
        {
            get
            {
                if (!this.HasValue)
                    return default(TType?);
                return baseRef.value;
            }
        }

        public bool HasValue { get; set; }

        [Newtonsoft.Json.JsonIgnore]
        public IRef<TType> Ref
        {
            get
            {
                if (!this.HasValue)
                    throw new Exception("Attempt to de-option empty value");
                return baseRef;
            }
        }
    }

    public struct RefObjOptional<TType> : IRefObjOptional<TType>
        where TType : class
    {
        private IRefObj<TType> baseRef;

        public static IRefObjOptional<TType> Empty()
        {
            return new RefObjOptional<TType>
            {
                HasValue = false,
                baseRef = default(IRefObj<TType>),
            };
        }

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
        where TType : struct, IReferenceable
    {
        public Refs(IEnumerableAsync<TType> valueTask)
        {
            this.ids = default(Guid[]);
            this.value = default(TType?);
            this.Values = valueTask;
            this.resolved = false;
        }

        public Refs(Guid[] ids) : this()
        {
            this.ids = ids;
            this.value = default(TType?);
            this.resolved = false;
        }

        public Refs(Guid[] ids,
            Func<Guid, Task<TType>> lookup) : this()
        {
            this.ids = ids;
            this.value = default(TType?);
            var index = 0;
            this.Values = EnumerableAsync.Yield<TType>(
                async (r, b) =>
                {
                    if (index >= ids.Length)
                        return b;
                    var id = ids[index];
                    index = index++;
                    var v = await lookup(id);
                    return r(v);
                });
            this.resolved = false;
        }

        public Guid[] ids { get; set; }

        public TType? value { get; set; }

        public bool resolved { get; set; }

        public IRef<TType>[] refs
        {
            get
            {
                if (!ids.Any())
                    return new IRef<TType>[] { };
                throw new NotImplementedException();
            }
        }

        public IEnumerableAsync<TType> Values
        {
            get;
            private set;
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

    public struct RefObjs<TType> : IRefObjs<TType>
        where TType : class
    {
        public Guid[] ids { get; private set; }

        public RefObjs(Guid[] ids) : this()
        {
            this.ids = ids;
        }

        public IRefObj<TType>[] refs
        {
            get
            {
                if (!ids.Any())
                    return new IRefObj<TType>[] { };
                return ids
                    .Select(id => (IRefObj<TType>)new RefObj<TType>(id))
                    .ToArray();
            }
        }

    }
}

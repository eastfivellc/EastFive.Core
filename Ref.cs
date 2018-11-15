using BlackBarLabs.Extensions;
using EastFive.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EastFive
{
    public interface IReferenceable
    {
        Guid id { get; }
    }

    public interface IRefBase : IReferenceable
    {
        Guid id { get; }
        
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

    public interface IReferences
    {
        Guid[] ids { get; }
    }

    public interface IRefs<TType> : IReferences
    {
        Linq.Async.IEnumerableAsync<TType> Values { get; }
    }

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

    public struct RefObj<TType>
        where TType : class
    {
        public Guid id;
        public TType value;
        public Task<TType> valueAsync;
        public bool resolved;
    }
}

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

    public interface IRefBase<TType>
    {
        Guid id { get; }
        
        Task<TResult> ValueAsync<TResult>(
            Func<TType, TResult> valueCallback);

        bool resolved { get; }
    }

    public interface IRef<TType> : IRefBase<TType>
        where TType : struct
    {
        TType? value { get; }
    }

    public interface IRefObj<TType> : IRefBase<TType>
        where TType : class
    {
        Func<TType> value { get; }
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
        
        public async Task<TResult> ValueAsync<TResult>(
            Func<TType, TResult> valueCallback)
        {
            if (value.HasValue)
                return valueCallback(value.Value);

            this.value = await valueTask;
            this.resolved = true;
            return valueCallback(value.Value);
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

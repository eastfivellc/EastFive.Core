using System;
using System.Threading.Tasks;

namespace EastFive.Serialization.Binding.Binders
{
    /// <summary>Binds <see cref="byte"/> arrays via <see cref="IBindingSource.GetBytes"/>.</summary>
    public sealed class BytesBinder : ITypeBinder
    {
        public bool CanBind(Type targetType) => targetType == typeof(byte[]);

        public ValueTask<TResult> Read<TResult>(
            Type targetType,
            IBindingSource source,
            IBindingContext context,
            Func<object, TResult> onBound,
            Func<BindFailure, TResult> onFailure,
            Func<TResult> onNull = null)
        {
            return source.GetBytes(v => onBound((object)v), onFailure, onNull);
        }

        public void Write(Type sourceType, object value, IBindingSink sink, IBindingContext context)
        {
            if (value is null) { sink.WriteNull(); return; }
            sink.WriteBytes((byte[])value);
        }
    }
}

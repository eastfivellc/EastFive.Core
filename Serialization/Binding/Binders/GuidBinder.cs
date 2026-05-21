using System;
using System.Threading.Tasks;

namespace EastFive.Serialization.Binding.Binders
{
    /// <summary>Binds <see cref="Guid"/> from <see cref="IBindingSource.GetGuid"/>.</summary>
    public sealed class GuidBinder : ITypeBinder
    {
        public bool CanBind(Type targetType) => targetType == typeof(Guid);

        public ValueTask<TResult> Read<TResult>(
            Type targetType,
            IBindingSource source,
            IBindingContext context,
            Func<object, TResult> onBound,
            Func<BindFailure, TResult> onFailure,
            Func<TResult> onNull = null)
        {
            return source.GetGuid(g => onBound((object)g), onFailure, onNull);
        }

        public void Write(Type sourceType, object value, IBindingSink sink, IBindingContext context)
        {
            if (value is null) { sink.WriteNull(); return; }
            sink.WriteGuid((Guid)value);
        }
    }
}

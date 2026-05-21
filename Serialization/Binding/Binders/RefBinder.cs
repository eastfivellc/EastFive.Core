using System;
using System.Threading.Tasks;

namespace EastFive.Serialization.Binding.Binders
{
    /// <summary>Binds <see cref="IRef{TType}"/> from a Guid value in the source.</summary>
    public sealed class RefBinder : ITypeBinder
    {
        public bool CanBind(Type targetType) =>
            targetType is { IsGenericType: true } &&
            targetType.GetGenericTypeDefinition() == typeof(IRef<>);

        public ValueTask<TResult> Read<TResult>(
            Type targetType,
            IBindingSource source,
            IBindingContext context,
            Func<object, TResult> onBound,
            Func<BindFailure, TResult> onFailure,
            Func<TResult> onNull = null)
        {
            var referenced = targetType.GetGenericArguments()[0];
            return source.GetGuid(
                g => onBound(g.BindToRef(referenced)),
                onFailure,
                onNull);
        }

        public void Write(Type sourceType, object value, IBindingSink sink, IBindingContext context)
        {
            if (value is null) { sink.WriteNull(); return; }
            var refValue = (IReferenceable)value;
            sink.WriteGuid(refValue.id);
        }
    }
}

using System;
using System.Threading.Tasks;

namespace EastFive.Serialization.Binding.Binders
{
    /// <summary>
    /// Binds <see cref="Nullable{T}"/>. Opts into null-handling by passing an
    /// <c>onNull</c> down to the underlying binder; a source-level null is then
    /// materialized as a boxed <c>null</c>. Otherwise delegates to the registered
    /// binder for the underlying value type.
    /// </summary>
    public sealed class NullableBinder : ITypeBinder
    {
        public bool CanBind(Type targetType) =>
            targetType is { IsGenericType: true } &&
            targetType.GetGenericTypeDefinition() == typeof(Nullable<>);

        public ValueTask<TResult> Read<TResult>(
            Type targetType,
            IBindingSource source,
            IBindingContext context,
            Func<object, TResult> onBound,
            Func<BindFailure, TResult> onFailure,
            Func<TResult> onNull = null)
        {
            var underlying = Nullable.GetUnderlyingType(targetType);
            return context.TypeBindings.Bind(underlying, source, context,
                onBound,
                onFailure,
                onNull: () => onBound(null));
        }

        public void Write(Type sourceType, object value, IBindingSink sink, IBindingContext context)
        {
            if (value is null) { sink.WriteNull(); return; }
            var underlying = Nullable.GetUnderlyingType(sourceType);
            context.TypeBindings.Emit(underlying, value, sink, context);
        }
    }
}

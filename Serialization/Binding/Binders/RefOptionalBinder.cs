using System;
using System.Threading.Tasks;

namespace EastFive.Serialization.Binding.Binders
{
    /// <summary>
    /// Binds <see cref="IRefOptional{TType}"/>. Source-level null is materialized as
    /// <see cref="RefOptionalHelper.CreateEmpty"/>; a Guid value wraps the resulting
    /// <c>IRef&lt;T&gt;</c> in a <c>RefOptional&lt;T&gt;</c>.
    /// </summary>
    public sealed class RefOptionalBinder : ITypeBinder
    {
        public bool CanBind(Type targetType) =>
            targetType is { IsGenericType: true } &&
            targetType.GetGenericTypeDefinition() == typeof(IRefOptional<>);

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
                onValue: g =>
                {
                    var baseRef = g.BindToRef(referenced);
                    var optional = Activator.CreateInstance(
                        typeof(RefOptional<>).MakeGenericType(referenced),
                        new[] { baseRef });
                    return onBound(optional);
                },
                onFailure: onFailure,
                onNull: () => onBound(RefOptionalHelper.CreateEmpty(referenced)));
        }

        public void Write(Type sourceType, object value, IBindingSink sink, IBindingContext context)
        {
            if (value is null) { sink.WriteNull(); return; }
            var optional = (IReferenceableOptional)value;
            if (optional.HasValue && optional.id.HasValue)
                sink.WriteGuid(optional.id.Value);
            else
                sink.WriteNull();
        }
    }
}

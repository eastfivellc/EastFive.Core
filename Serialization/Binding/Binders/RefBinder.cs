using System;
using System.Threading.Tasks;

namespace EastFive.Serialization.Binding.Binders
{
    /// <summary>Binds <c>IRef&lt;T&gt;</c>. Accepts native Guid or native string (parse).</summary>
    public sealed class RefBinder : ITypeBinder
    {
        public bool CanBind(Type targetType) =>
            targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(IRef<>);

        public ValueTask<TResult> Read<TResult>(
            Type targetType,
            IBindingSource source,
            IBindingContext context,
            Func<object, TResult> onBound,
            Func<BindFailure, TResult> onFailure,
            Func<TResult> onNull = null)
        {
            var path = context?.KeyPath;
            var referenced = targetType.GetGenericArguments()[0];

            return source.GetValue<TResult>(
                path: path,
                onNull: onNull,
                onGuid: g => onBound(g.BindToRef(referenced)),
                onString: s =>
                {
                    if (Guid.TryParse(s, out var g))
                        return onBound(g.BindToRef(referenced));
                    return onFailure(new BindFailure(new ParseError($"'{s}' is not a Guid"), targetType, path));
                },
                onFailure: onFailure);
        }

        public void Write(Type sourceType, object value, IBindingSink sink, IBindingContext context)
        {
            if (value is null) { sink.WriteNull(); return; }
            var guidProp = value.GetType().GetProperty("id");
            var g = (Guid)guidProp.GetValue(value);
            sink.WriteGuid(g);
        }
    }
}

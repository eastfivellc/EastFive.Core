using System;
using System.Threading.Tasks;

namespace EastFive.Serialization.Binding.Binders
{
    /// <summary>Binds any <see cref="Enum"/> from a string (name) in the source.</summary>
    public sealed class EnumBinder : ITypeBinder
    {
        public bool CanBind(Type targetType) => targetType is { IsEnum: true };

        public ValueTask<TResult> Read<TResult>(
            Type targetType,
            IBindingSource source,
            IBindingContext context,
            Func<object, TResult> onBound,
            Func<BindFailure, TResult> onFailure,
            Func<TResult> onNull = null)
        {
            return source.GetString(
                s =>
                {
                    if (Enum.TryParse(targetType, s, ignoreCase: true, out var value))
                        return onBound(value);

                    var valid = string.Join(", ", Enum.GetNames(targetType));
                    return onFailure(new BindFailure(
                        new ParseError($"'{s}' is not a valid value for `{targetType.FullName}`. Valid values: [{valid}]."),
                        targetType, context.KeyPath));
                },
                onFailure,
                onNull);
        }

        public void Write(Type sourceType, object value, IBindingSink sink, IBindingContext context)
        {
            if (value is null) { sink.WriteNull(); return; }
            sink.WriteString(value.ToString());
        }
    }
}

using System;
using System.Threading.Tasks;

namespace EastFive.Serialization.Binding.Binders
{
    /// <summary>Binds <see cref="DateTime"/> via <see cref="IBindingSource.GetDateTime"/>.</summary>
    public sealed class DateTimeBinder : ITypeBinder
    {
        public bool CanBind(Type targetType) => targetType == typeof(DateTime);

        public ValueTask<TResult> Read<TResult>(
            Type targetType,
            IBindingSource source,
            IBindingContext context,
            Func<object, TResult> onBound,
            Func<BindFailure, TResult> onFailure,
            Func<TResult> onNull = null)
        {
            return source.GetDateTime(v => onBound((object)v), onFailure, onNull);
        }

        public void Write(Type sourceType, object value, IBindingSink sink, IBindingContext context)
        {
            if (value is null) { sink.WriteNull(); return; }
            sink.WriteDateTime((DateTime)value);
        }
    }

    /// <summary>Binds <see cref="DateTimeOffset"/> from the source's DateTime accessor.</summary>
    public sealed class DateTimeOffsetBinder : ITypeBinder
    {
        public bool CanBind(Type targetType) => targetType == typeof(DateTimeOffset);

        public ValueTask<TResult> Read<TResult>(
            Type targetType,
            IBindingSource source,
            IBindingContext context,
            Func<object, TResult> onBound,
            Func<BindFailure, TResult> onFailure,
            Func<TResult> onNull = null)
        {
            return source.GetDateTime(v =>
            {
                var dt = v.Kind == DateTimeKind.Unspecified
                    ? DateTime.SpecifyKind(v, DateTimeKind.Utc)
                    : v;
                return onBound((object)new DateTimeOffset(dt));
            }, onFailure, onNull);
        }

        public void Write(Type sourceType, object value, IBindingSink sink, IBindingContext context)
        {
            if (value is null) { sink.WriteNull(); return; }
            var dto = (DateTimeOffset)value;
            sink.WriteDateTime(dto.UtcDateTime);
        }
    }
}

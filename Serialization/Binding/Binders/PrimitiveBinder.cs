using System;
using System.Threading.Tasks;

namespace EastFive.Serialization.Binding.Binders
{
    /// <summary>
    /// Binds the core scalar primitives that map 1:1 to <see cref="IBindingSource"/>
    /// accessors: <c>string</c>, <c>bool</c>, integer widths, floating point, decimal,
    /// and <see cref="Uri"/>.
    /// </summary>
    public sealed class PrimitiveBinder : ITypeBinder
    {
        public bool CanBind(Type targetType) =>
            targetType == typeof(string) ||
            targetType == typeof(bool) ||
            targetType == typeof(sbyte) ||
            targetType == typeof(byte) ||
            targetType == typeof(short) ||
            targetType == typeof(ushort) ||
            targetType == typeof(int) ||
            targetType == typeof(uint) ||
            targetType == typeof(long) ||
            targetType == typeof(ulong) ||
            targetType == typeof(float) ||
            targetType == typeof(double) ||
            targetType == typeof(decimal) ||
            targetType == typeof(Uri) ||
            targetType == typeof(char);

        public ValueTask<TResult> Read<TResult>(
            Type targetType,
            IBindingSource source,
            IBindingContext context,
            Func<object, TResult> onBound,
            Func<BindFailure, TResult> onFailure,
            Func<TResult> onNull = null)
        {
            if (targetType == typeof(string))
                return source.GetString(s => onBound((object)s), onFailure, onNull);

            if (targetType == typeof(bool))
                return source.GetBool(v => onBound((object)v), onFailure, onNull);

            if (targetType == typeof(long))
                return source.GetInt64(v => onBound((object)v), onFailure, onNull);

            if (targetType == typeof(int))
                return source.GetInt64(v =>
                {
                    if (v < int.MinValue || v > int.MaxValue)
                        return onFailure(new BindFailure(new ParseError($"{v} is out of range for Int32"), targetType, context.KeyPath));
                    return onBound((object)(int)v);
                }, onFailure, onNull);

            if (targetType == typeof(short))
                return source.GetInt64(v => onBound((object)(short)v), onFailure, onNull);

            if (targetType == typeof(ushort))
                return source.GetInt64(v => onBound((object)(ushort)v), onFailure, onNull);

            if (targetType == typeof(byte))
                return source.GetInt64(v => onBound((object)(byte)v), onFailure, onNull);

            if (targetType == typeof(sbyte))
                return source.GetInt64(v => onBound((object)(sbyte)v), onFailure, onNull);

            if (targetType == typeof(uint))
                return source.GetInt64(v => onBound((object)(uint)v), onFailure, onNull);

            if (targetType == typeof(ulong))
                return source.GetInt64(v => onBound((object)(ulong)v), onFailure, onNull);

            if (targetType == typeof(double))
                return source.GetDouble(v => onBound((object)v), onFailure, onNull);

            if (targetType == typeof(float))
                return source.GetDouble(v => onBound((object)(float)v), onFailure, onNull);

            if (targetType == typeof(decimal))
                return source.GetDouble(v => onBound((object)(decimal)v), onFailure, onNull);

            if (targetType == typeof(char))
                return source.GetString(s =>
                {
                    if (s.Length == 1) return onBound((object)s[0]);
                    return onFailure(new BindFailure(new ParseError($"'{s}' is not a single character"), targetType, context.KeyPath));
                }, onFailure, onNull);

            if (targetType == typeof(Uri))
                return source.GetString(s =>
                {
                    if (Uri.TryCreate(s, UriKind.RelativeOrAbsolute, out var uri))
                        return onBound((object)uri);
                    return onFailure(new BindFailure(new ParseError($"'{s}' is not a valid URI"), targetType, context.KeyPath));
                }, onFailure, onNull);

            return new ValueTask<TResult>(onFailure(new BindFailure(
                new UnsupportedTargetType(targetType), targetType, context.KeyPath)));
        }

        public void Write(Type sourceType, object value, IBindingSink sink, IBindingContext context)
        {
            if (value is null) { sink.WriteNull(); return; }
            if (sourceType == typeof(string))    { sink.WriteString((string)value); return; }
            if (sourceType == typeof(bool))      { sink.WriteBool((bool)value); return; }
            if (sourceType == typeof(sbyte))     { sink.WriteInt64((sbyte)value); return; }
            if (sourceType == typeof(byte))      { sink.WriteInt64((byte)value); return; }
            if (sourceType == typeof(short))     { sink.WriteInt64((short)value); return; }
            if (sourceType == typeof(ushort))    { sink.WriteInt64((ushort)value); return; }
            if (sourceType == typeof(int))       { sink.WriteInt64((int)value); return; }
            if (sourceType == typeof(uint))      { sink.WriteInt64((uint)value); return; }
            if (sourceType == typeof(long))      { sink.WriteInt64((long)value); return; }
            if (sourceType == typeof(ulong))     { sink.WriteInt64((long)(ulong)value); return; }
            if (sourceType == typeof(float))     { sink.WriteDouble((float)value); return; }
            if (sourceType == typeof(double))    { sink.WriteDouble((double)value); return; }
            if (sourceType == typeof(decimal))   { sink.WriteDouble((double)(decimal)value); return; }
            if (sourceType == typeof(char))      { sink.WriteString(((char)value).ToString()); return; }
            if (sourceType == typeof(Uri))       { sink.WriteString(((Uri)value).ToString()); return; }
            throw new InvalidOperationException($"PrimitiveBinder cannot write {sourceType.FullName}.");
        }
    }
}

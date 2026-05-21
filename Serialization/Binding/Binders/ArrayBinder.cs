using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EastFive.Serialization.Binding.Binders
{
    /// <summary>
    /// Binds CLR arrays (<c>T[]</c>) by iterating the source as an array and binding
    /// each element via the active <see cref="ITypeBindings"/>.
    /// </summary>
    public sealed class ArrayBinder : ITypeBinder
    {
        public bool CanBind(Type targetType) =>
            targetType is { IsArray: true } && targetType != typeof(byte[]);

        public ValueTask<TResult> Read<TResult>(
            Type targetType,
            IBindingSource source,
            IBindingContext context,
            Func<object, TResult> onBound,
            Func<BindFailure, TResult> onFailure,
            Func<TResult> onNull = null)
        {
            var elementType = targetType.GetElementType();
            return source.GetArray(
                items =>
                {
                    var list = (IList)Array.CreateInstance(elementType, 0);
                    var buffer = new List<object>();
                    BindFailure? firstFailure = null;
                    var index = 0;
                    foreach (var item in items)
                    {
                        var local = index++;
                        var childContext = context.WithKeyPath(
                            string.IsNullOrEmpty(context.KeyPath) ? $"[{local}]" : $"{context.KeyPath}[{local}]");

                        var done = context.TypeBindings.Bind<bool>(elementType, item, childContext,
                                v => { buffer.Add(v); return true; },
                                f => { firstFailure = f; return false; })
                            .GetAwaiter().GetResult();
                        if (!done) break;
                    }
                    if (firstFailure.HasValue)
                        return onFailure(firstFailure.Value);

                    var arr = Array.CreateInstance(elementType, buffer.Count);
                    for (var i = 0; i < buffer.Count; i++)
                        arr.SetValue(buffer[i], i);
                    return onBound(arr);
                },
                onFailure,
                onNull);
        }

        public void Write(Type sourceType, object value, IBindingSink sink, IBindingContext context)
        {
            if (value is null) { sink.WriteNull(); return; }
            var elementType = sourceType.GetElementType();
            var arr = (IEnumerable)value;
            foreach (var item in arr)
            {
                var itemSink = sink.AppendItem();
                context.TypeBindings.Emit(elementType, item, itemSink, context);
            }
        }
    }
}

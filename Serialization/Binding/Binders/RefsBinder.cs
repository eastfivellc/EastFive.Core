using System;
using System.Linq;
using System.Threading.Tasks;

namespace EastFive.Serialization.Binding.Binders
{
    /// <summary>Binds <see cref="IRefs{TType}"/> from an array of Guids in the source.</summary>
    public sealed class RefsBinder : ITypeBinder
    {
        public bool CanBind(Type targetType) =>
            targetType is { IsGenericType: true } &&
            targetType.GetGenericTypeDefinition() == typeof(IRefs<>);

        public ValueTask<TResult> Read<TResult>(
            Type targetType,
            IBindingSource source,
            IBindingContext context,
            Func<object, TResult> onBound,
            Func<BindFailure, TResult> onFailure,
            Func<TResult> onNull = null)
        {
            var referenced = targetType.GetGenericArguments()[0];

            return source.GetArray(
                items =>
                {
                    var ids = new System.Collections.Generic.List<Guid>();
                    BindFailure? firstFailure = null;
                    var index = 0;
                    foreach (var item in items)
                    {
                        var local = index++;
                        var continueLoop = item.GetGuid<bool>(
                                g => { ids.Add(g); return true; },
                                f => { firstFailure = f.Nest($"[{local}]"); return false; })
                            .GetAwaiter().GetResult();
                        if (!continueLoop) break;
                    }
                    if (firstFailure.HasValue)
                        return onFailure(firstFailure.Value);

                    var refsType = typeof(Refs<>).MakeGenericType(referenced);
                    var instance = Activator.CreateInstance(refsType, new object[] { ids.ToArray() });
                    return onBound(instance);
                },
                onFailure,
                onNull);
        }

        public void Write(Type sourceType, object value, IBindingSink sink, IBindingContext context)
        {
            if (value is null) { sink.WriteNull(); return; }
            var refs = (IReferences)value;
            foreach (var id in refs.ids ?? Array.Empty<Guid>())
                sink.AppendItem().WriteGuid(id);
        }
    }
}

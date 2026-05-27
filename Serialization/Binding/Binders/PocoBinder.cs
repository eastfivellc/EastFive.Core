using System;
using System.Threading.Tasks;

namespace EastFive.Serialization.Binding.Binders
{
    /// <summary>
    /// Catch-all binder for complex types: probes the source for object shape, then
    /// for each <see cref="MemberPlan"/> recurses against the SAME root source with
    /// <see cref="IBindingContext.KeyPath"/> advanced to the member's wire-name.
    /// Missing members surface as <see cref="NotPresent"/> and are tolerated;
    /// other failures bubble up.
    /// </summary>
    public sealed class PocoBinder : ITypeBinder
    {
        public bool CanBind(Type targetType) =>
            targetType is { IsClass: true } &&
            targetType != typeof(string) &&
            !targetType.IsArray &&
            targetType.GetConstructor(Type.EmptyTypes) is not null;

        public async ValueTask<TResult> Read<TResult>(
            Type targetType,
            IBindingSource source,
            IBindingContext context,
            Func<object, TResult> onBound,
            Func<BindFailure, TResult> onFailure,
            Func<TResult> onNull = null)
        {
            var path = context?.KeyPath ?? string.Empty;

            // Probe shape and null-ness in one call.
            var (kind, navFailure) = await source.GetValue<(int kind, BindFailure? failure)>(
                path: path,
                onNull: () => (1, (BindFailure?)null),       // kind 1 = null
                onObject: _ => (2, (BindFailure?)null),      // kind 2 = object
                onFailure: f => (0, (BindFailure?)f));

            if (navFailure is { } nf)
            {
                if (nf.Reason is NotPresent && onNull is not null) return onNull();
                return onFailure(nf);
            }

            if (kind == 1)
            {
                if (onNull is not null) return onNull();
                return onFailure(new BindFailure(new NullValue(), targetType, path));
            }
            // kind == 2 → object

            var provider = context.MemberPlanProvider
                ?? throw new InvalidOperationException("PocoBinder requires an IMemberPlanProvider on the context.");
            var plan = provider.GetPlan(targetType);
            var instance = Activator.CreateInstance(targetType);

            foreach (var member in plan)
            {
                var memberPath = string.IsNullOrEmpty(path) ? member.WireName : $"{path}.{member.WireName}";
                var memberContext = context.WithKeyPath(memberPath).WithSlot(member.Slot);
                var memberType = MemberType(member);

                BindFailure? failure = null;
                object bound = null;
                var memberBindings = memberContext.TypeBindings.ForSlot(member.Slot);
                var ok = await memberBindings.Bind<bool>(
                    memberType,
                    source,
                    memberContext,
                    v => { bound = v; return true; },
                    f => { failure = f; return false; },
                    onNull: () => { bound = null; return true; });

                if (!ok)
                {
                    if (failure?.Reason is NotPresent) continue; // tolerate missing
                    var inner = new BindFailure(failure.Value.Reason, failure.Value.ExpectedType, member.WireName);
                    return onFailure(new BindFailure(new NestedFailure(inner), targetType, path));
                }
                member.Setter(instance, bound);
            }

            return onBound(instance);
        }

        public void Write(Type sourceType, object value, IBindingSink sink, IBindingContext context)
        {
            if (value is null) { sink.WriteNull(); return; }
            var provider = context.MemberPlanProvider
                ?? throw new InvalidOperationException("PocoBinder requires an IMemberPlanProvider on the context.");
            var plan = provider.GetPlan(sourceType);
            foreach (var member in plan)
            {
                var child = sink.Scope(member.WireName);
                var memberType = MemberType(member);
                var memberValue = member.Getter(value);
                context.TypeBindings.Emit(memberType, memberValue, child, context);
            }
        }

        private static Type MemberType(MemberPlan member) => member.Slot.Type;
    }
}

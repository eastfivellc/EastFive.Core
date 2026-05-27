using System.Globalization;

namespace EastFive.Serialization.Binding
{
    /// <summary>
    /// Ambient information carried alongside a bind call: the active type-binding
    /// table, the slot being filled (if any), a dotted key path for diagnostics,
    /// and culture. Capabilities (e.g. <c>IApplication</c>) are deliberately not
    /// part of this interface — they're injected when constructing the
    /// <see cref="ITypeBindings"/> via factory methods like
    /// <see cref="ITypeBindings.ForSlot"/> or assembly-specific entry points.
    /// </summary>
    public interface IBindingContext
    {
        ITypeBindings TypeBindings { get; }

        /// <summary>The slot currently being filled, if any (POCO member, method parameter).</summary>
        IBindingSlot Slot { get; }

        string KeyPath { get; }

        CultureInfo Culture { get; }

        /// <summary>
        /// Scope-specific member selector consulted by <c>PocoBinder</c>. Determines
        /// <em>which</em> members of a complex type are bindable and under what
        /// wire-names. Orthogonal to <see cref="ITypeBindings"/>, which determines
        /// <em>how</em> a single value is bound.
        /// </summary>
        IMemberPlanProvider MemberPlanProvider { get; }

        IBindingContext WithSlot(IBindingSlot slot);

        IBindingContext WithKeyPath(string keyPath);

        IBindingContext WithTypeBindings(ITypeBindings typeBindings);

        IBindingContext WithMemberPlanProvider(IMemberPlanProvider memberPlanProvider);
    }
}

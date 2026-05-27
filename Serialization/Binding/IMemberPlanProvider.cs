using System;
using System.Collections.Generic;

namespace EastFive.Serialization.Binding
{
    /// <summary>
    /// A single bindable member produced by an <see cref="IMemberPlanProvider"/>.
    /// Pairs an <see cref="IBindingSlot"/> (reflection identity + attribute provider)
    /// with the wire-name that <c>PocoBinder</c> uses against
    /// <see cref="IBindingSource.GetScoped{TResult}"/> / <see cref="IBindingSink.Scope(string)"/>,
    /// plus the cached accessor delegates used to read/write the member on an instance.
    ///
    /// <para>
    /// <see cref="IBindingSlot.Name"/> remains the .NET member name (used by
    /// <see cref="ITypeBindings.ForSlot"/> for per-member binder overrides);
    /// <see cref="WireName"/> is what the source/sink format actually sees.
    /// </para>
    /// </summary>
    public sealed class MemberPlan
    {
        public MemberPlan(
            IBindingSlot slot,
            string wireName,
            Action<object, object> setter,
            Func<object, object> getter)
        {
            Slot = slot ?? throw new ArgumentNullException(nameof(slot));
            WireName = wireName ?? throw new ArgumentNullException(nameof(wireName));
            Setter = setter ?? throw new ArgumentNullException(nameof(setter));
            Getter = getter ?? throw new ArgumentNullException(nameof(getter));
        }

        public IBindingSlot Slot { get; }
        public string WireName { get; }
        public Action<object, object> Setter { get; }
        public Func<object, object> Getter { get; }
    }

    /// <summary>
    /// <b>Scope-specific member selection</b> for complex types. Where
    /// <see cref="ITypeBindings"/> answers <em>"which binder for type X?"</em>,
    /// <see cref="IMemberPlanProvider"/> answers <em>"which members of type X are bindable
    /// in this scope, and under what wire-names?"</em>.
    ///
    /// <para>
    /// Carried on <see cref="IBindingContext.MemberPlanProvider"/>. Each scope
    /// supplies its own:
    /// <list type="bullet">
    ///   <item><c>ConventionalMemberPlanProvider</c> (EastFive.Core) — public R/W
    ///         properties + non-readonly public fields, with attribute-name-string
    ///         lookups for <c>JsonProperty</c>/<c>DataMember</c>/<c>Property</c>
    ///         name overrides and <c>JsonIgnore</c>/<c>NonSerialized</c>/<c>IgnoreDataMember</c>
    ///         skips. Default for <c>TypeBindings.Default</c>.</item>
    ///   <item><c>ColumnMemberPlanProvider</c> (EastFive.Azure, future) — walks
    ///         <c>[Column]</c>/<c>IPersistInAzureStorageTables</c>.</item>
    ///   <item><c>ApiPropertyMemberPlanProvider</c> (EastFive.Api, future) — walks
    ///         <c>[Property]</c>/<c>IProvideApiValue</c>.</item>
    /// </list>
    /// </para>
    ///
    /// <para>Implementations are responsible for caching by <see cref="System.Type"/>;
    /// <c>PocoBinder</c> calls <see cref="GetPlan"/> on every bind.</para>
    /// </summary>
    public interface IMemberPlanProvider
    {
        IReadOnlyList<MemberPlan> GetPlan(Type targetType);
    }
}

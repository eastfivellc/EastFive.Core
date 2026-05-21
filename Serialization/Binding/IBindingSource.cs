using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EastFive.Serialization.Binding
{
    /// <summary>
    /// Format-neutral, async-shaped read accessor for a single value. Each format
    /// (string, JSON, EDM/EntityProperty, form/query) implements this once;
    /// <see cref="ITypeBinder"/> implementations consume <em>only</em> this surface,
    /// keeping them format-agnostic.
    /// <para>
    /// All accessors use the TResult pattern carried over <see cref="ValueTask{TResult}"/>.
    /// Sync implementations should return <c>new ValueTask&lt;TResult&gt;(value)</c>
    /// for zero-allocation completion.
    /// </para>
    /// <para>
    /// <b>Three outcome channels:</b>
    /// <list type="bullet">
    ///   <item><c>onValue</c> — typed value successfully produced.</item>
    ///   <item><c>onNull</c> — source represents an explicit null. Optional: when not
    ///   supplied, the source instead invokes <c>onFailure</c> with a <see cref="NullValue"/>
    ///   reason, so binders that don't model null get correct propagation by default.</item>
    ///   <item><c>onFailure</c> — value present but cannot be produced in the requested form
    ///   (parse error, wrong source kind, etc.). Never raised as an exception.</item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Null vs. absent</b> are distinct: "absent" means the parent's
    /// <see cref="GetScoped{TResult}"/> emits <see cref="NotPresent"/> (no child source
    /// produced); "null" means a child source exists but represents an explicit null,
    /// which surfaces via <c>onNull</c> (or <see cref="NullValue"/> failure if no
    /// <c>onNull</c> was supplied).
    /// </para>
    /// <para>
    /// Composite accessors (<see cref="GetScoped{TResult}"/>,
    /// <see cref="GetIndexed{TResult}"/>, <see cref="GetArray{TResult}"/>,
    /// <see cref="GetMembers{TResult}"/>) on a non-composite source must report
    /// <see cref="WrongSourceType"/>.
    /// </para>
    /// </summary>
    public interface IBindingSource
    {
        ValueTask<TResult> GetString<TResult>(
            Func<string, TResult> onValue,
            Func<BindFailure, TResult> onFailure,
            Func<TResult> onNull = null);

        ValueTask<TResult> GetGuid<TResult>(
            Func<Guid, TResult> onValue,
            Func<BindFailure, TResult> onFailure,
            Func<TResult> onNull = null);

        ValueTask<TResult> GetBool<TResult>(
            Func<bool, TResult> onValue,
            Func<BindFailure, TResult> onFailure,
            Func<TResult> onNull = null);

        ValueTask<TResult> GetInt64<TResult>(
            Func<long, TResult> onValue,
            Func<BindFailure, TResult> onFailure,
            Func<TResult> onNull = null);

        ValueTask<TResult> GetDouble<TResult>(
            Func<double, TResult> onValue,
            Func<BindFailure, TResult> onFailure,
            Func<TResult> onNull = null);

        ValueTask<TResult> GetDateTime<TResult>(
            Func<DateTime, TResult> onValue,
            Func<BindFailure, TResult> onFailure,
            Func<TResult> onNull = null);

        ValueTask<TResult> GetBytes<TResult>(
            Func<byte[], TResult> onValue,
            Func<BindFailure, TResult> onFailure,
            Func<TResult> onNull = null);

        /// <summary>Access a named child (object member). Fails with <see cref="NotPresent"/> if the key is missing on a real object source, <see cref="WrongSourceType"/> if the source isn't object-shaped.</summary>
        ValueTask<TResult> GetScoped<TResult>(
            string key,
            Func<IBindingSource, TResult> onChild,
            Func<BindFailure, TResult> onFailure,
            Func<TResult> onNull = null);

        /// <summary>Access a positional child (array element). Fails with <see cref="NotPresent"/> if out-of-range, <see cref="WrongSourceType"/> if the source isn't array-shaped.</summary>
        ValueTask<TResult> GetIndexed<TResult>(
            int index,
            Func<IBindingSource, TResult> onChild,
            Func<BindFailure, TResult> onFailure,
            Func<TResult> onNull = null);

        ValueTask<TResult> GetArray<TResult>(
            Func<IEnumerable<IBindingSource>, TResult> onItems,
            Func<BindFailure, TResult> onFailure,
            Func<TResult> onNull = null);

        ValueTask<TResult> GetMembers<TResult>(
            Func<IEnumerable<KeyValuePair<string, IBindingSource>>, TResult> onMembers,
            Func<BindFailure, TResult> onFailure,
            Func<TResult> onNull = null);
    }
}

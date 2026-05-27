using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace EastFive.Serialization.Binding
{
    /// <summary>
    /// Default <see cref="IMemberPlanProvider"/>: convention-based, format-neutral,
    /// dependency-free member discovery. Used by <c>TypeBindings.Default</c>.
    ///
    /// <para>
    /// Discovery rules:
    /// <list type="bullet">
    ///   <item>Public instance properties with both a getter and a setter (non-indexed).</item>
    ///   <item>Public non-readonly, non-literal instance fields.</item>
    ///   <item>Members carrying an attribute whose type name is one of
    ///         <c>JsonIgnoreAttribute</c> / <c>NonSerializedAttribute</c> /
    ///         <c>IgnoreDataMemberAttribute</c> are skipped.</item>
    ///   <item>Wire-name is overridden by the first attribute named
    ///         <c>JsonPropertyAttribute</c> / <c>DataMemberAttribute</c> /
    ///         <c>PropertyAttribute</c> that exposes a non-empty
    ///         <c>PropertyName</c> or <c>Name</c> property.</item>
    /// </list>
    /// Attribute matching is by simple type name to keep EastFive.Core free of
    /// Newtonsoft / EastFive.Api dependencies.
    /// </para>
    ///
    /// <para>Plans are cached per <see cref="Type"/> on the singleton instance.</para>
    /// </summary>
    public sealed class ConventionalMemberPlanProvider : IMemberPlanProvider
    {
        public static ConventionalMemberPlanProvider Instance { get; } = new ConventionalMemberPlanProvider();

        private readonly ConcurrentDictionary<Type, MemberPlan[]> cache = new();

        public IReadOnlyList<MemberPlan> GetPlan(Type targetType) =>
            cache.GetOrAdd(targetType ?? throw new ArgumentNullException(nameof(targetType)), BuildPlan);

        private static MemberPlan[] BuildPlan(Type targetType)
        {
            var props = targetType
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && p.CanWrite && p.GetIndexParameters().Length == 0)
                .Where(NotIgnored)
                .Select(p => new MemberPlan(
                    new MemberSlot(p),
                    WireNameFor(p, p.Name),
                    (inst, v) => p.SetValue(inst, v),
                    inst => p.GetValue(inst)));

            var fields = targetType
                .GetFields(BindingFlags.Public | BindingFlags.Instance)
                .Where(f => !f.IsInitOnly && !f.IsLiteral)
                .Where(NotIgnored)
                .Select(f => new MemberPlan(
                    new MemberSlot(f),
                    WireNameFor(f, f.Name),
                    (inst, v) => f.SetValue(inst, v),
                    inst => f.GetValue(inst)));

            return props.Concat(fields).ToArray();
        }

        private static bool NotIgnored(MemberInfo m)
        {
            foreach (var a in m.GetCustomAttributes(inherit: true))
            {
                var n = a.GetType().Name;
                if (n == "JsonIgnoreAttribute" || n == "NonSerializedAttribute" || n == "IgnoreDataMemberAttribute")
                    return false;
            }
            return true;
        }

        private static string WireNameFor(MemberInfo m, string fallback)
        {
            foreach (var a in m.GetCustomAttributes(inherit: true))
            {
                var t = a.GetType();
                if (t.Name != "JsonPropertyAttribute" && t.Name != "DataMemberAttribute" && t.Name != "PropertyAttribute")
                    continue;
                var prop = t.GetProperty("PropertyName") ?? t.GetProperty("Name");
                if (prop is null) continue;
                var v = prop.GetValue(a) as string;
                if (!string.IsNullOrEmpty(v)) return v;
            }
            return fallback;
        }
    }
}

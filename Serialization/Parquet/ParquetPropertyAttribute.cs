﻿using System;
using System.Linq;
using System.Reflection;

using Parquet.Data;

using EastFive.Extensions;
using EastFive.Linq;
using EastFive.Reflection;

namespace EastFive.Serialization.Parquet
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true)]
    public class ParquetPropertyAttribute : ScopedMapParquetPropertyAttribute
    {
        public string Name { get; set; }

        public StringComparison ComparisonType { get; set; }

        public ParquetPropertyAttribute()
        {
        }

        public ParquetPropertyAttribute(string name)
        {
            this.Name = name;
        }

        public override TResource ParseMemberValueFromRow<TResource>(TResource resource,
            MemberInfo member, (global::Parquet.Data.Field key, object value)[] rowValues)
        {
            var type = member.GetPropertyOrFieldType();
            var nameToMatch = this.Name.HasBlackSpace() ?
                this.Name
                :
                member.Name;
            var assignment = rowValues
                .Where(tpl => String.Equals(nameToMatch, tpl.key.Name, ComparisonType))
                .First(
                    (rowKeyValue, next) =>
                    {
                        var (rowKey, rowValue) = rowKeyValue;
                        var assign = ParseAssignment<TResource>(type, member, rowValue, this.ComparisonType);
                        return assign;
                    },
                    () =>
                    {
                        Func<TResource, TResource> assign = (res) => res;
                        return assign;
                    });
            var updatedResource = assignment(resource);
            return updatedResource;
        }

        public static Func<TResource, TResource> ParseAssignment<TResource>(Type type, MemberInfo member, object rowValue,
            StringComparison comparisonType)
        {
            try
            {
                if (type.TryGetNullableUnderlyingType(out Type baseType))
                {
                    return ParseAssignment<TResource>(baseType, member, rowValue, comparisonType);
                }

                if (!rowValue.TryGetType(out Type rowValueType))
                {
                    Func<TResource, TResource> noAssign = (res) => res;
                    return noAssign;
                }

                if (type.IsAssignableFrom(rowValueType))
                {
                    Func<TResource, TResource> assign = (res) =>
                        (TResource)member.SetPropertyOrFieldValue(res, rowValue);
                    return assign;
                }

                if (typeof(string).IsAssignableFrom(rowValueType))
                {
                    var strValue = (string)rowValue;
                    return Text.TextPropertyAttribute.ParseAssignment<TResource>(type, member, strValue, comparisonType);
                }

                if (typeof(DateTime).IsAssignableFrom(rowValueType))
                {
                    var memberType = member.GetPropertyOrFieldType();
                    var dtValue = memberType.IsNullable()?
                        (rowValue.IsDefaultOrNull()?
                            default(DateTime?)
                            :
                            (DateTime?)rowValue)
                        :
                        (DateTime)rowValue;
                    if (memberType.IsAssignableFrom(typeof(DateTime)))
                    {
                        Func<TResource, TResource> assign = (res) =>
                            (TResource)member.SetPropertyOrFieldValue(res, dtValue);
                        return assign;
                    }
                }

                if (typeof(DateTimeOffset).IsAssignableFrom(rowValueType))
                {
                    var dtoValue = (DateTimeOffset)rowValue;
                    var dtValue = dtoValue.DateTime;
                    var memberType = member.GetPropertyOrFieldType();
                    if (memberType.IsAssignableFrom(typeof(DateTime)))
                    {
                        Func<TResource, TResource> assign = (res) =>
                            (TResource)member.SetPropertyOrFieldValue(res, dtValue);
                        return assign;
                    }
                }

                if (rowValue.TryCastObjRef(type, out object parsedRefObj))
                {
                    Func<TResource, TResource> assign = (res) =>
                        (TResource)member.SetPropertyOrFieldValue(res, parsedRefObj);
                    return assign;
                }
            } catch(Exception)
            {
                return (r) => r;
            }

            throw new Exception($"{nameof(ParquetPropertyAttribute)} cannot parse {type.FullName} on {member.DeclaringType.FullName}..{member.Name}");

        }
    }

    public class ParquetPropertyDebugAttribute : ParquetPropertyAttribute
    {
        public override bool DoesMap(string scope)
        {
            return base.DoesMap(scope);
        }

        public override bool Match(object obj)
        {
            return base.Match(obj);
        }

        public override TResource ParseMemberValueFromRow<TResource>(TResource resource, MemberInfo member,
            (Field key, object value)[] rowValues)
        {
            return base.ParseMemberValueFromRow(resource, member, rowValues);
        }
    }
}


using System;
using System.Linq;
using System.Reflection;
using EastFive.Extensions;
using EastFive.Linq;
using EastFive.Reflection;

namespace EastFive.Serialization.Parquet
{
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

        public override TResource ParseRow<TResource>(TResource resource,
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
            return assignment(resource);
        }

        public static Func<TResource, TResource> ParseAssignment<TResource>(Type type, MemberInfo member, object rowValue,
            StringComparison comparisonType)
        {
            if(!rowValue.TryGetType(out Type rowValueType))
            {
                Func<TResource, TResource> noAssign = (res) => res;
                return noAssign;
            }

            if(type.IsAssignableFrom(rowValueType))
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

            if (rowValue.TryCastObjRef(type, out object parsedRefObj))
            {
                Func<TResource, TResource> assign = (res) =>
                    (TResource)member.SetPropertyOrFieldValue(res, parsedRefObj);
                return assign;
            }

            return type.IsNullable(
                baseType => ParseAssignment<TResource>(baseType, member, rowValue, comparisonType),
                () =>
                {
                    throw new Exception($"{nameof(ParquetPropertyAttribute)} cannot parse {type.FullName} on {member.DeclaringType.FullName}..{member.Name}");
                });

            
        }
    }

    public class ParquetProperty2Attribute : ParquetPropertyAttribute { };
    public class ParquetProperty3Attribute : ParquetPropertyAttribute { };
    public class ParquetProperty4Attribute : ParquetPropertyAttribute { };
    public class ParquetProperty5Attribute : ParquetPropertyAttribute { };
    public class ParquetProperty6Attribute : ParquetPropertyAttribute { };
    public class ParquetProperty7Attribute : ParquetPropertyAttribute { };
    public class ParquetProperty8Attribute : ParquetPropertyAttribute { };
}


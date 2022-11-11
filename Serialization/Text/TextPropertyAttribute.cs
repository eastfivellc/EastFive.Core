using System;
using System.Linq;
using System.Reflection;

using EastFive.Linq;
using EastFive.Reflection;

namespace EastFive.Serialization.Text
{
    public class TextPropertyAttribute : ScopedMapTextPropertyAttribute
    {
        public string Name { get; set; }

        public StringComparison ComparisonType { get; set; }

        public override TResource ParseRow<TResource>(TResource resource,
            MemberInfo member, (string key, string value)[] rowValues)
        {
            var type = member.GetPropertyOrFieldType();

            var assignment = rowValues
                .Where(tpl => String.Equals(this.Name, tpl.key, ComparisonType))
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

        public static Func<TResource, TResource> ParseAssignment<TResource>(Type type, MemberInfo member, string rowValue,
            StringComparison comparisonType)
        {
            if (typeof(string).IsAssignableTo(type))
            {
                Func<TResource, TResource> assign = (res) =>
                    (TResource)member.SetPropertyOrFieldValue(res, rowValue);
                return assign;
            }
            if (typeof(Guid).IsAssignableTo(type))
            {
                Guid.TryParse(rowValue, out Guid guidValue);
                Func<TResource, TResource> assign = (res) =>
                    (TResource)member.SetPropertyOrFieldValue(res, guidValue);
                return assign;
            }
            if (typeof(DateTime).IsAssignableTo(type))
            {
                DateTime.TryParse(rowValue, out DateTime dtValue);
                Func<TResource, TResource> assign = (res) =>
                    (TResource)member.SetPropertyOrFieldValue(res, dtValue);
                return assign;
            }
            if (typeof(bool).IsAssignableTo(type))
            {
                bool.TryParse(rowValue, out bool boolValue);
                Func<TResource, TResource> assign = (res) =>
                    (TResource)member.SetPropertyOrFieldValue(res, boolValue);
                return assign;
            }
            if (type.IsEnum)
            {
                var ignoreCase = comparisonType == StringComparison.OrdinalIgnoreCase
                    || comparisonType == StringComparison.InvariantCultureIgnoreCase
                    || comparisonType == StringComparison.CurrentCultureIgnoreCase;
                if (Enum.TryParse(type, rowValue, ignoreCase, out object newValue))
                {
                    Func<TResource, TResource> assign = (res) =>
                        (TResource)member.SetPropertyOrFieldValue(res, newValue);
                    return assign;
                }
                Func<TResource, TResource> noAssign = (res) => res;
                return noAssign;
            }
            if (rowValue.TryParseRef(type, out object refObj))
            {
                Func<TResource, TResource> assign = (res) =>
                    (TResource)member.SetPropertyOrFieldValue(res, rowValue);
                return assign;
            }
            throw new Exception($"{nameof(TextPropertyAttribute)} cannot parse {type.FullName} on {member.DeclaringType.FullName}..{member.Name}");
        }
    }

    public class TextProperty2Attribute : TextPropertyAttribute { };
    public class TextProperty3Attribute : TextPropertyAttribute { };
    public class TextProperty4Attribute : TextPropertyAttribute { };
    public class TextProperty5Attribute : TextPropertyAttribute { };
    public class TextProperty6Attribute : TextPropertyAttribute { };
    public class TextProperty7Attribute : TextPropertyAttribute { };
    public class TextProperty8Attribute : TextPropertyAttribute { };
}


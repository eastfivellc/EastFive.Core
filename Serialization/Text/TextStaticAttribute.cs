using System;
using System.Reflection;

using EastFive.Reflection;

namespace EastFive.Serialization.Text
{
    public class TextStaticAttribute : ScopedMapTextPropertyAttribute
    {
        public string Value { get; set; }

        public StringComparison ComparisonType { get; set; }

        public override TResource ParseRow<TResource>(TResource resource,
            MemberInfo member, (string key, string value)[] rowValues)
        {
            var type = member.GetPropertyOrFieldType();
            var assignment = TextPropertyAttribute.ParseAssignment<TResource>(type, member, this.Value, ComparisonType);
            return assignment(resource);
        }
    }

    public class TextStatic2Attribute : TextStaticAttribute { }
    public class TextStatic3Attribute : TextStaticAttribute { }
    public class TextStatic4Attribute : TextStaticAttribute { }
    public class TextStatic5Attribute : TextStaticAttribute { }
    public class TextStatic6Attribute : TextStaticAttribute { }
    public class TextStatic7Attribute : TextStaticAttribute { }
}


using System;
using System.Reflection;

using EastFive.Reflection;

namespace EastFive.Serialization.Text
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true)]
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

    //public class TextStaticAttribute : TextStaticAttribute { }
    //public class TextStaticAttribute : TextStaticAttribute { }
    //public class TextStaticAttribute : TextStaticAttribute { }
    //public class TextStaticAttribute : TextStaticAttribute { }
    //public class TextStaticAttribute : TextStaticAttribute { }
    //public class TextStaticAttribute : TextStaticAttribute { }
}


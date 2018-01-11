using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EastFive
{
    public static class CustomAttributeExtensions
    {
        public static T[] GetCustomAttributes<T>(this Type type, bool inherit = false)
            where T : class
        {
            var attributes = type.GetCustomAttributes(typeof(T), inherit);
            return attributes.Select(attrib => attrib as T).ToArray();
        }

        public static bool ContainsCustomAttribute<T>(this Type type, bool inherit = false)
            where T : class
        {
            var attributes = type.GetCustomAttributes<T>(inherit);
            return attributes.Count() > 0;
        }

        public static T[] GetCustomAttributes<T>(this System.Reflection.MemberInfo type, bool inherit = false)
            where T : System.Attribute
        {
            var attributes = type.GetCustomAttributes(typeof(T), inherit);
            return attributes.Select(attrib => attrib as T).ToArray();
        }

        public static bool ContainsCustomAttribute<T>(this System.Reflection.MemberInfo type, bool inherit = false)
            where T : System.Attribute
        {
            var attributes = type.GetCustomAttributes<T>(inherit);
            return attributes.Count() > 0;
        }

        public static TResult GetCustomAttribute<TAttribute, TResult>(this System.Reflection.MemberInfo obj,
            Func<TAttribute, TResult> onHasAttribute,
            Func<TResult> onAttributeNotOnObject,
            bool inherit = false)
            where TAttribute : System.Attribute
        {
            var attributesUncast = obj.GetCustomAttributes(typeof(TAttribute), inherit);
            var attributes = attributesUncast.Select(attrib => attrib as TAttribute).ToArray();
            if (!attributes.Any())
                return onAttributeNotOnObject();
            return onHasAttribute(attributes.First());
        }
    }
}

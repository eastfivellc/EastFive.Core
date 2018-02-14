using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
            return attributes.Any();
        }

        public static T GetCustomAttribute<T>(this Type type, bool inherit = false)
            where T : class
        {
            var attributes = type.GetCustomAttributes(typeof(T), inherit);
            return attributes.Select(attrib => attrib as T).First();
        }

        public static TResult GetCustomAttribute<TAttribute, TResult>(this Type obj,
            Func<TAttribute, TResult> onHasAttribute,
            Func<TResult> onAttributeNotOnObject,
            bool inherit = false)
            where TAttribute : System.Attribute
        {
            var attributesUncast = obj.GetCustomAttributes(typeof(TAttribute), inherit);
            //var attributes = attributesUncast.Select(attrib => attrib as TAttribute).ToArray();
            var attributes = attributesUncast.Cast<TAttribute>().ToArray();
            if (!attributes.Any())
                return onAttributeNotOnObject();
            return onHasAttribute(attributes.First());
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
            return attributes.Any();
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

        public static T[] GetCustomAttributes<T>(this MethodInfo method, bool inherit = false)
            where T : class
        {
            var attributes = method.GetCustomAttributes(typeof(T), inherit);
            return attributes.Select(attrib => attrib as T).ToArray();
        }

        public static bool ContainsCustomAttribute<T>(this MethodInfo method, bool inherit = false)
            where T : class
        {
            var attributes = method.GetCustomAttributes<T>(inherit);
            return attributes.Any();
        }

        public static T[] GetCustomAttributes<T>(this ParameterInfo method, bool inherit = false)
            where T : class
        {
            var attributes = method.GetCustomAttributes(typeof(T), inherit);
            return attributes.Select(attrib => attrib as T).ToArray();
        }

        public static bool ContainsCustomAttribute<T>(this ParameterInfo method, bool inherit = false)
            where T : class
        {
            var attributes = method.GetCustomAttributes<T>(inherit);
            return attributes.Any();
        }

        //public static bool ContainsCustomAttribute<T>(this ParameterInfo parameter, bool inherit = false)
        //    where T : System.Attribute
        //{
        //    var attributes = type.GetCustomAttributes<T>(inherit);
        //    return attributes.Any();
        //}
    }
}

using EastFive.Extensions;
using EastFive.Linq;
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

        public static T[] GetCustomAttributes<T>(this System.Reflection.MemberInfo type,
                bool inherit = false)
            where T : System.Attribute
        {
            var attributes = type.GetCustomAttributes(typeof(T), inherit);
            var castAttrs = attributes.Select(attrib => attrib as T).ToArray();
            return castAttrs;
        }

        public static bool ContainsCustomAttribute<T>(this System.Reflection.MemberInfo type, bool inherit = false)
            where T : System.Attribute
        {
            var attributes = type.GetCustomAttributes<T>(inherit);
            return attributes.Any();
        }

        public static bool ContainsAttributeInterface<T>(this System.Reflection.MemberInfo type, bool inherit = false)
        {
            if (!typeof(T).IsInterface)
                throw new ArgumentException($"{typeof(T).FullName} is not an interface.");
            var attributes = type.GetCustomAttributes(inherit)
                .Where(attr => attr.GetType().IsSubClassOfGeneric(typeof(T)));
            return attributes.Any();
        }

        public static T GetAttributeInterface<T>(this System.Reflection.MemberInfo type,
            bool inherit = false)
        {
            if (!typeof(T).IsInterface)
                throw new ArgumentException($"{typeof(T).FullName} is not an interface.");
            var attributes = type.GetCustomAttributes(inherit)
                .Where(attr => attr.GetType().IsSubClassOfGeneric(typeof(T)))
                .Select(attr => (T)attr)
                .ToArray();
            return attributes.First<T, T>(
                (attr, discard) => attr,
                () => throw new ArgumentException($"{type.DeclaringType}..{type.Name} does not contain an attribute of type {typeof(T).FullName}."));
        }

        public static T[] GetAttributesInterface<T>(this System.Reflection.MemberInfo type,
            bool inherit = false,
            bool multiple = false)
        {
            if (!typeof(T).IsInterface)
                throw new ArgumentException($"{typeof(T).FullName} is not an interface.");
            var attributes = type.GetCustomAttributes(inherit)
                .Where(attr => attr.GetType().IsSubClassOfGeneric(typeof(T)))
                .Select(attr => (T)attr)
                .ToArray();
            if(!multiple)
                return attributes;
            if (!(type is Type))
                return attributes;
            var typeType = type as Type;
            if(typeType.BaseType.IsDefaultOrNull())
                return attributes;
            var baseAttrs = typeType.BaseType.GetAttributesInterface<T>(inherit, multiple);
            return attributes.Concat(baseAttrs).Distinct().ToArray();
        }

        public static T[] GetAttributesInterface<T>(this System.Reflection.MethodInfo method, bool inherit = false)
        {
            if (!typeof(T).IsInterface)
                throw new ArgumentException($"{typeof(T).FullName} is not an interface.");
            var attributes = method.GetCustomAttributes(inherit)
                .Where(attr => attr.GetType().IsSubClassOfGeneric(typeof(T)))
                .Select(attr => (T)attr);
            return attributes.ToArray();
        }

        public static T[] GetAttributesInterface<T>(this System.Reflection.ParameterInfo type, bool inherit = false)
        {
            if (!typeof(T).IsInterface)
                throw new ArgumentException($"{typeof(T).FullName} is not an interface.");
            var attributes = type.GetCustomAttributes(inherit)
                .Where(attr => attr.GetType().IsSubClassOfGeneric(typeof(T)))
                .Select(attr => (T)attr);
            return attributes.ToArray();
        }

        public static T GetAttributeInterface<T>(this System.Reflection.ParameterInfo type, bool inherit = false)
        {
            if (!typeof(T).IsInterface)
                throw new ArgumentException($"{typeof(T).FullName} is not an interface.");
            var attributes = type.GetCustomAttributes(inherit)
                .Where(attr => attr.GetType().IsSubClassOfGeneric(typeof(T)))
                .Select(attr => (T)attr);
            return attributes.First();
        }

        public static bool ContainsAttributeInterface<T>(this System.Reflection.ParameterInfo type, bool inherit = false)
        {
            if (!typeof(T).IsInterface)
                throw new ArgumentException($"{typeof(T).FullName} is not an interface.");
            var attributes = type.GetCustomAttributes(inherit)
                .Where(attr => attr.GetType().IsSubClassOfGeneric(typeof(T)))
                .Select(attr => (T)attr);
            return attributes.Any();
        }

        public static bool ContainsCustomAttribute(this System.Reflection.MemberInfo type, 
            Type attributeType, bool inherit = false)
        {
            var attributes = type.GetCustomAttributes(attributeType, inherit);
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

        public static TAttribute GetCustomAttribute<TAttribute>(this MemberInfo obj,
            bool inherit = false)
            where TAttribute : System.Attribute
        {
            var attributesUncast = obj.GetCustomAttributes(typeof(TAttribute), inherit);
            //var attributes = attributesUncast.Select(attrib => attrib as TAttribute).ToArray();
            var attributes = attributesUncast.Cast<TAttribute>().ToArray();
            if (!attributes.Any())
                throw new Exception("Attribute not found.");
            return (attributes.First());
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
        
        public static TResult GetCustomAttribute<TAttribute, TResult>(this ParameterInfo obj,
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

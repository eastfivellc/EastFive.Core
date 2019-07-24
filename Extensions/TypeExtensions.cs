using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using EastFive.Extensions;

namespace EastFive
{
    public static class TypeExtensions
    {
        public static object ConvertObject(this Type type, object obj)
        {
            if (type == typeof(string))
                return obj.ToString();
            if (type == typeof(short))
                return Convert.ToInt16(obj);
            if (type == typeof(int))
                return Convert.ToInt32(obj);
            if (type == typeof(long))
                return Convert.ToInt64(obj);
            if (type == typeof(bool))
                return Convert.ToBoolean(obj);
            if (type == typeof(decimal))
                return Convert.ToDecimal(obj);
            if (type == typeof(double))
                return Convert.ToDouble(obj);
            if (type == typeof(float))
                return Convert.ToDecimal(obj);
            throw new ArgumentException("Unsupported conversion type:" + type.FullName);
        }

        public static object GetDefault(this Type t)
        {
            Func<object> f = GetDefault<object>;
            return f.Method.GetGenericMethodDefinition().MakeGenericMethod(t).Invoke(null, null);
        }

        private static T GetDefault<T>()
        {
            return default(T);
        }

        public static bool IsSubClassOfGeneric(this Type child, Type parent)
        {
            if (child == parent)
                return false;

            if (child.IsSubclassOf(parent))
                return true;

            var parameters = parent.GetGenericArguments();
            var isParameterLessGeneric = !(parameters != null && parameters.Length > 0 &&
                ((parameters[0].Attributes & TypeAttributes.BeforeFieldInit) == TypeAttributes.BeforeFieldInit));

            while (child != null && child != typeof(object))
            {
                var cur = GetFullTypeDefinition(child);
                if (parent == cur || (isParameterLessGeneric && cur.GetInterfaces().Select(i => GetFullTypeDefinition(i)).Contains(GetFullTypeDefinition(parent))))
                    return true;
                else if (!isParameterLessGeneric)
                    if (GetFullTypeDefinition(parent) == cur && !cur.IsInterface)
                    {
                        if (VerifyGenericArguments(GetFullTypeDefinition(parent), cur))
                            if (VerifyGenericArguments(parent, child))
                                return true;
                    }
                    else
                        foreach (var item in child.GetInterfaces().Where(i => GetFullTypeDefinition(parent) == GetFullTypeDefinition(i)))
                            if (VerifyGenericArguments(parent, item))
                                return true;

                child = child.BaseType;
            }

            return false;
        }

        private static Type GetFullTypeDefinition(Type type)
        {
            return type.IsGenericType ? type.GetGenericTypeDefinition() : type;
        }

        private static bool VerifyGenericArguments(Type parent, Type child)
        {
            Type[] childArguments = child.GetGenericArguments();
            Type[] parentArguments = parent.GetGenericArguments();
            if (childArguments.Length == parentArguments.Length)
                for (int i = 0; i < childArguments.Length; i++)
                    if (childArguments[i].Assembly != parentArguments[i].Assembly || childArguments[i].Name != parentArguments[i].Name || childArguments[i].Namespace != parentArguments[i].Namespace)
                        if (!childArguments[i].IsSubclassOf(parentArguments[i]))
                            return false;

            return true;
        }

        public static bool IsNullable(this Type type)
        {
            // https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/nullable-types/how-to-identify-a-nullable-type
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
        }

        public static TResult IsNullable<TResult>(this Type type,
            Func<Type, TResult> onNullable,
            Func<TResult> onNotNullable)
        {
            if (!type.IsNullable())
                return onNotNullable();
            return onNullable(Nullable.GetUnderlyingType(type));
        }

        public static bool IsNumeric(this Type type)
        {
            if (type.IsWholeNumber())
                return true;
            if (type.IsDecimalNumber())
                return true;
            return false;
        }

        public static bool IsWholeNumber(this Type type)
        {
            if (!type.IsAssignableFrom(typeof(int)))
                return true;
            if (!type.IsAssignableFrom(typeof(long)))
                return true;
            if (!type.IsAssignableFrom(typeof(short)))
                return true;
            if (!type.IsAssignableFrom(typeof(byte)))
                return true;
            return false;
        }

        public static bool IsDecimalNumber(this Type type)
        {
            if (!type.IsAssignableFrom(typeof(float)))
                return true;
            if (!type.IsAssignableFrom(typeof(double)))
                return true;
            if (!type.IsAssignableFrom(typeof(decimal)))
                return true;
            return false;
        }

        public static Type MakeArrayType(this Type t)
        {
            var discardArray = Array.CreateInstance(t, 0);
            var arrayType = discardArray.GetType();
            return arrayType;
        }
    }
}

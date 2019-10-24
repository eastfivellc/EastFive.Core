using EastFive.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace EastFive.Reflection
{
    public static class ReflectionExtensions
    {
        public static IEnumerable<MemberInfo> GetPropertyOrFieldMembers(this Type type)
        {
            return type
                .GetMembers()
                .Where(
                    member =>
                    {
                        if (member.MemberType == MemberTypes.Property)
                            return true;
                        if (member.MemberType == MemberTypes.Field)
                            return true;
                        return false;
                    });
        }

        public static Type GetPropertyOrFieldType(this MemberInfo memberType)
        {
            if(memberType is PropertyInfo)
                return (memberType as PropertyInfo).PropertyType;

            if (memberType is FieldInfo)
                return (memberType as FieldInfo).FieldType;

            throw new ArgumentException("memberType",
                $"Cannot determine type of {memberType.GetType().FullName} since it could not be casted to {typeof(PropertyInfo).Name} or {typeof(FieldInfo).Name}.");
        }

        public static object GetPropertyOrFieldValue(this MemberInfo memberInfo, object obj)
        {
            if (memberInfo is PropertyInfo)
                return (memberInfo as PropertyInfo).GetValue(obj);

            if (memberInfo is FieldInfo)
                return (memberInfo as FieldInfo).GetValue(obj);

            throw new ArgumentException("memberType",
                $"Cannot determine type of {memberInfo.GetType().FullName} since it could not be casted to {typeof(PropertyInfo).Name} or {typeof(FieldInfo).Name}.");
        }

        public static object GetPropertyOrField(this object obj, string propertyOrFieldName)
        {
            return obj
                .GetType()
                .GetProperty(propertyOrFieldName)
                .GetValue(obj)
                .ToString();
        }

        public static object Cast(this IEnumerable<object> enumerableOfObj, Type enumerableType)
        {
            var typeConvertedEnumerable = typeof(System.Linq.Enumerable)
                .GetMethod("Cast", BindingFlags.Static | BindingFlags.Public)
                .MakeGenericMethod(new Type[] { enumerableType })
                .Invoke(null, new object[] { enumerableOfObj });

            return typeConvertedEnumerable;
        }

        public static object CastArray(this IEnumerable<object> arrayOfObj, Type arrayType)
        {
            var typeConvertedEnumerable = arrayOfObj.Cast(arrayType);
            var typeConvertedArray = typeof(System.Linq.Enumerable)
                .GetMethod("ToArray", BindingFlags.Static | BindingFlags.Public)
                .MakeGenericMethod(new Type[] { arrayType })
                .Invoke(null, new object[] { typeConvertedEnumerable });
            return typeConvertedArray;
        }

        public static IEnumerable<object> ObjectToEnumerable(this object arrayOfObj)
        {
            var enumerable = (System.Collections.IEnumerable)arrayOfObj;
            foreach (var item in enumerable)
                yield return item;
        }

        public static IEnumerable<KeyValuePair<object, object>> DictionaryKeyValuePairs(this object dictionary)
        {
            if (!dictionary.GetType().IsSubClassOfGeneric(typeof(IDictionary<,>)))
                throw new ArgumentException($"{dictionary.GetType().FullName} is not of type IDictionary<>");

            foreach (var kvpObj in (dictionary as System.Collections.IEnumerable))
            {
                var kvpType = kvpObj.GetType();
                var keyProperty = kvpType.GetProperty("Key");
                var keyValue = keyProperty.GetValue(kvpObj);
                var valueProperty = kvpObj.GetType().GetProperty("Value");
                var valueValue = valueProperty.GetValue(kvpObj);
                yield return valueValue.PairWithKey(keyValue);
            }
        }

        public static bool IsExtension(this MethodInfo method)
        {
            return method.IsDefaultOrNull() ?
                false
                :
                method.IsDefined(typeof(System.Runtime.CompilerServices.ExtensionAttribute), false);
        }

        public static bool NullableHasValue(this object valueTypeValue)
        {
            // ?
            if (valueTypeValue == null)
                return false;

            var underlyingType = valueTypeValue.GetType();

            var valueProp = underlyingType.GetProperty("HasValue",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            return (bool)valueProp.GetValue(valueTypeValue);
        }

        public static object GetNullableValue(this object valueTypeValue)
        {
            // ?
            //if (valueTypeValue == null)
            //    return null;

            var underlyingType = valueTypeValue.GetType();

            // If the value is not Nullable.Value == false, it is just the value.
            // Yeah, Nullable is an odd thing is C#
            if (!underlyingType.IsNullable())
                return valueTypeValue;

            var valueProp = underlyingType.GetProperty("Value",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            return valueProp.GetValue(valueTypeValue);
        }
    }
}

using BlackBarLabs.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace EastFive.Linq.Expressions
{
    public static class ExpressionExtensions
    {
        public static TResult PropertyName<TObject, TProperty, TResult>(this Expression<Func<TObject, TProperty>> propertyExpression,
            Func<string, TResult> onPropertyExpression,
            Func<TResult> onNotPropertyExpression)
        {
            if (propertyExpression.Body.IsDefault())
                return onNotPropertyExpression();

            if (!(propertyExpression.Body is MemberExpression))
                return onNotPropertyExpression();

            var memberExpression = propertyExpression.Body as MemberExpression;
            var lockedPropertyMember = memberExpression.Member;
            
            var propertyInfo = lockedPropertyMember as PropertyInfo;
            if (null == propertyInfo)
            {
                return onNotPropertyExpression();
            }
            return onPropertyExpression(propertyInfo.Name);
        }

        public static TResult MemberName<TObject, TProperty, TResult>(this Expression<Func<TObject, TProperty>> memberExpression,
            Func<string, TResult> onMemberExpression,
            Func<TResult> onNotMemberExpression)
        {
            if (memberExpression.Body.IsDefault())
                return onNotMemberExpression();

            if (!(memberExpression.Body is MemberExpression))
                return onNotMemberExpression();

            var memberExpressionTyped = memberExpression.Body as MemberExpression;
            var member = memberExpressionTyped.Member;
            return onMemberExpression(member.Name);
        }

        public static object GetValue(this MemberInfo memberInfo, object obj)
        {
            switch (memberInfo.MemberType)
            {
                case MemberTypes.Field:
                    return ((FieldInfo)memberInfo).GetValue(obj);
                case MemberTypes.Property:
                    return ((PropertyInfo)memberInfo).GetValue(obj);
                default:
                    throw new ArgumentException($"memberInfo of type '{memberInfo.MemberType}' which is unsupported");
            }
        }

        public static void SetValue<T>(this MemberInfo memberInfo, ref T obj, object value)
        {
            // unbox in case of struct
            object objUnboxed = obj;
            switch (memberInfo.MemberType)
            {
                case MemberTypes.Field:
                    ((FieldInfo)memberInfo).SetValue(objUnboxed, value);
                    break;
                case MemberTypes.Property:
                    ((PropertyInfo)memberInfo).SetValue(obj, value);
                    break;
                default:
                    throw new ArgumentException($"memberInfo of type '{memberInfo.MemberType}' which is unsupported");
            }
            obj = (T)objUnboxed;
        }
    }
}

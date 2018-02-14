using BlackBarLabs.Extensions;
using EastFive.Extensions;
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
            return propertyExpression.PropertyInfo(
                propertyInfo => onPropertyExpression(propertyInfo.Name),
                onNotPropertyExpression);
        }

        public static TResult PropertyInfo<TObject, TProperty, TResult>(this Expression<Func<TObject, TProperty>> propertyExpression,
            Func<PropertyInfo, TResult> onPropertyExpression,
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
            return onPropertyExpression(propertyInfo);
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

        public static KeyValuePair<ParameterInfo, object>[] ResolveArgs(this MethodCallExpression body)
        {
            var values = body.Arguments
                .Zip(
                    body.Method.GetParameters(),
                    (arg, paramInfo) => paramInfo.PairWithValue(arg))
                .Select(argument => argument.Key.PairWithValue(ResolveMemberExpression(argument.Value)))
                .ToArray();
            return values;
        }

        private static object ResolveMemberExpression(Expression expression)
        {
            if (expression is MemberExpression)
                return GetMemberExpressionValue((MemberExpression)expression);

            if (expression is UnaryExpression)
                // if casting is involved, Expression is not x => x.FieldName but x => Convert(x.Fieldname)
                return GetMemberExpressionValue((MemberExpression)((UnaryExpression)expression).Operand);

            var value = Expression.Lambda(expression).Compile().DynamicInvoke();
            return value;
        }

        private static object GetMemberExpressionValue(MemberExpression exp)
        {
            // expression is ConstantExpression or FieldExpression
            if (exp.Expression is ConstantExpression)
                return (((ConstantExpression)exp.Expression).Value)
                        .GetType()
                        .GetField(exp.Member.Name)
                        .GetValue(((ConstantExpression)exp.Expression).Value);

            if (exp.Expression is MemberExpression)
                return GetMemberExpressionValue((MemberExpression)exp.Expression);

            var value = Expression.Lambda(exp.Expression).Compile().DynamicInvoke();
            return value;
        }
    }
}

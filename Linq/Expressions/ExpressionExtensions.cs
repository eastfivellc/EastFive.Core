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

        public static Type GetMemberType(this MemberInfo memberInfo)
        {
            switch (memberInfo.MemberType)
            {
                case MemberTypes.Field:
                    return ((FieldInfo)memberInfo).FieldType;
                case MemberTypes.Property:
                    return ((PropertyInfo)memberInfo).PropertyType;
                default:
                    throw new ArgumentException($"memberInfo of type '{memberInfo.MemberType}' which is unsupported");
            }
        }


        public static KeyValuePair<ParameterInfo, object>[] ResolveArgs(this MethodCallExpression body)
        {
            var values = body.Arguments
                .Zip(
                    body.Method.GetParameters(),
                    (arg, paramInfo) => paramInfo.PairWithValue(arg))
                .Select(argument => argument.Key.PairWithValue(Resolve(argument.Value)))
                .ToArray();
            return values;
        }

        /// <summary>
        /// Attempt to resolve the value of the expression without compiling and invoking the expression.
        /// </summary>
        /// <remarks>
        /// This method will compile and invoke the expression,
        /// or a subset of the expression, 
        /// if it fails to resolve it with other means.
        /// Methods called by the expression will be invoked.
        /// </remarks>
        /// <param name="expression"></param>
        /// <returns></returns>
        public static object Resolve(this Expression expression)
        {
            if (expression is MemberExpression)
            {
                var memberExpression = expression as MemberExpression;
                if (!memberExpression.Expression.IsDefaultOrNull())
                {
                    var memberObject = memberExpression.Expression.Resolve();
                    //var memberOfObjectAccessed = memberObject.GetType().GetField(memberExpression.Member.Name);
                    //var memberValue = memberOfObjectAccessed.GetValue(memberObject);
                    var memberValue = memberExpression.Member.GetValue(memberObject);
                    return memberValue;
                }
            }

            if (expression is UnaryExpression)
            {
                // if casting is involved, Expression is not value but Convert(value)
                var unaryExpression = expression as UnaryExpression;
                // or possibly some other method than convert, either way, just get the method
                var unaryMethod = unaryExpression.Method;
                // and ensure it's static and only has one parameter
                if(unaryMethod.IsStatic && unaryMethod.GetParameters().Length == 1)
                {
                    var operandValue = unaryExpression.Operand.Resolve();
                    try
                    {
                        var convertedValue = unaryExpression.Method.Invoke(null, new object[] { operandValue });
                        return convertedValue;
                    } catch(Exception ex)
                    {
                        ex.GetType();
                    }
                }
                // otherwise more work is required to call it.
                
            }

            if (expression is ConstantExpression)
            {
                var constantExpression = expression as ConstantExpression;
                var constantValue = constantExpression.Value;
                return constantValue;
            }

            var value = Expression.Lambda(expression).Compile().DynamicInvoke();
            return value;
        }
        
        public static TResult GetAssignment<TObject, TResult>(this Expression<Action<TObject>> expression,
            Func<MemberInfo, object, TResult> onAssignmentResolved,
            Func<string, TResult> onFailure = default(Func<string, TResult>))
        {
            var body = expression.Body;
            var methodCall = body as MethodCallExpression;

            var memberInfo = (methodCall.Arguments[0] as MemberExpression).Member;
            var valueResolved = methodCall.Arguments[1].Resolve();
            
            return onAssignmentResolved(memberInfo, valueResolved);
        }
    }
}

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
        public static Type GetPropertyOrFieldType(this MemberInfo memberType)
        {
            if(memberType is PropertyInfo)
                return (memberType as PropertyInfo).PropertyType;

            if (memberType is FieldInfo)
                return (memberType as FieldInfo).FieldType;

            throw new ArgumentException("memberType",
                $"Cannot determine type of {memberType.GetType().FullName} since it could not be casted to {typeof(PropertyInfo).Name} or {typeof(FieldInfo).Name}.");

        }

        [Obsolete("This may not be possible")]
        private static KeyValuePair<Type, object>[] ResolveArgs<T>(this Expression<Func<T, object>> expression)
        {
            var body = (System.Linq.Expressions.MethodCallExpression)expression.Body;
            var values = new List<KeyValuePair<Type, object>>();

            return values.ToArray();
        }

        public static object ResolveMemberExpression(this Expression expression)
        {
            if (expression is MemberExpression)
            {
                return GetValue((MemberExpression)expression);
            }

            if (expression is UnaryExpression)
            {
                // if casting is involved, Expression is not x => x.FieldName but x => Convert(x.Fieldname)
                return GetValue((MemberExpression)((UnaryExpression)expression).Operand);
            }

            if (expression is ParameterExpression)
            {
                // if casting is involved, Expression is not x => x.FieldName but x => Convert(x.Fieldname)
                var value = Expression.Lambda(expression as ParameterExpression).Compile().DynamicInvoke();
                return value;
            }

            if (expression is LambdaExpression)
            {
                var lambdaExpression = expression as System.Linq.Expressions.LambdaExpression;
                return null;
            }

            try
            {
                var value = Expression.Lambda(expression).Compile().DynamicInvoke();
                return value;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        private static object GetValue(this MemberExpression exp)
        {
            // expression is ConstantExpression or FieldExpression
            if (exp.Expression is ConstantExpression)
            {
                return (((ConstantExpression)exp.Expression).Value)
                        .GetType()
                        .GetField(exp.Member.Name)
                        .GetValue(((ConstantExpression)exp.Expression).Value);
            }

            if (exp.Expression is MemberExpression)
            {
                return GetValue((MemberExpression)exp.Expression);
            }

            if (exp.Expression is MethodCallExpression)
            {
                try
                {
                    var value = Expression.Lambda(exp.Expression as MethodCallExpression).Compile().DynamicInvoke();
                    return value;
                }
                catch (Exception ex)
                {
                    ex.GetType();
                }
            }

            throw new NotImplementedException();
        }
    }
}

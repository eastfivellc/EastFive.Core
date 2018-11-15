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

        public static object ResolveExpression(this Expression expression)
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
                throw new Exception($"Exception while attempting to execute expression:{expression.ToString()}", ex);
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
        
        public static object Cast(this IEnumerable<object> values, Type castTo)
        {
            //var list = new List<object>(values);
            //list.Add(x);
            var listOfTypeType = typeof(List<>).MakeGenericType(castTo);
            var addMethod = listOfTypeType.GetMethod("Add");
            var instance = Activator.CreateInstance(listOfTypeType);
            foreach (var value in values)
                addMethod.Invoke(instance, new object[] { value });
            var array = listOfTypeType.GetMethod("ToArray").Invoke(instance, new object[] { });
            return array;
        }
    }
}

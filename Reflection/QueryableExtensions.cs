using EastFive.Extensions;
using EastFive.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace EastFive.Reflection
{
    public static class QueryableExtensions
    {
        public static TAggr Compile<TAggr, TMethodAttribute>(this IQueryable urlQuery,
                TAggr startingValue,
            Func<TAggr, TMethodAttribute, MethodInfo, Expression[], TAggr> onRecognizedAttribute,
            Func<TAggr, MethodInfo, Expression[], TAggr> onUnrecognizedAttribute)
        {
            var expression = urlQuery.Expression;
            //var provider = urlQuery.Provider;
            return FlattenArgumentExpression(expression)
                .Aggregate(startingValue,
                    (aggr, methodArgsKvp) =>
                    {
                        var method = methodArgsKvp.Key;
                        var argumentExpressions = methodArgsKvp.Value;
                        var methodAttrs = method.GetAttributesInterface<TMethodAttribute>();
                        if (methodAttrs.Any())
                        {
                            var methodAttr = methodAttrs.First();
                            return onRecognizedAttribute(aggr, methodAttr, method, argumentExpressions);
                        }

                        if (method.DeclaringType == typeof(System.Linq.Queryable))
                            return onUnrecognizedAttribute(aggr, method, argumentExpressions);
                        
                        throw new ArgumentException($"Cannot compile Method `{method.DeclaringType.FullName}..{method.Name}`");
                    });
        }

        private static IEnumerable<KeyValuePair<MethodInfo, Expression[]>> FlattenArgumentExpression(Expression argExpression)
        {
            if (argExpression is MethodCallExpression)
            {
                var methodCallExpression = argExpression as MethodCallExpression;
                var method = methodCallExpression.Method;
                var isExtensionMethod = method.IsExtension();
                if (isExtensionMethod)
                {
                    foreach (var subExpr in FlattenArgumentExpression(methodCallExpression.Arguments.First()))
                        yield return subExpr;
                }
                var nonExtensionThisArgs = methodCallExpression.Arguments
                    .If(isExtensionMethod, args => args.Skip(1))
                    .Where(arg => !(arg is MethodCallExpression))
                    .ToArray();
                yield return methodCallExpression.Method
                    .PairWithValue(nonExtensionThisArgs);
            }
            yield break;
        }
    }
}

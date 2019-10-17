using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using EastFive.Linq.Expressions;

namespace EastFive.Reflection
{
    public static class ExpressionExtensions
    {
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

        public static object GetValue(this MemberExpression exp)
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

        public static MemberInfo MemberComparison(this Expression expression, out ExpressionType relationship, out object value)
        {
            var result = expression.MemberComparison(
                (memberInfo, r, v) =>
                    new
                    {
                        memberInfo,
                        r,
                        v,
                    },
                () => throw new NotImplementedException());
            relationship = result.r;
            value = result.v;
            return result.memberInfo;
        }

        public static TResult MemberComparison<TResult>(this Expression expression,
            Func<MemberInfo, ExpressionType, object, TResult> onResolved,
            Func<TResult> onNotResolved)
        {
            if (expression is UnaryExpression)
            {
                var argUnary = expression as UnaryExpression;
                return MemberComparison(argUnary.Operand,
                    onResolved,
                    onNotResolved);
            }
            if (expression is Expression<Func<object>>)
            {
                var paramExpr = expression as Expression<Func<object>>;
                return MemberComparison(paramExpr.Body,
                    onResolved,
                    onNotResolved);
            }
            if (expression is BinaryExpression)
            {
                var binaryExpr = expression as BinaryExpression;
                if (binaryExpr.Left is MemberExpression)
                {
                    var left = binaryExpr.Left as MemberExpression;
                    var memberInfo = left.Member;
                    var relationship = binaryExpr.NodeType;
                    var value = binaryExpr.Right.Resolve();
                    return onResolved(memberInfo, relationship, value);
                }
            }
            return onNotResolved();
        }
    }
}

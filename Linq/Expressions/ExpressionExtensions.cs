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
    }
}

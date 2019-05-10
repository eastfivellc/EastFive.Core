using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace EastFive.Linq
{
    public abstract class QueryProvider<TQueryable> : IQueryProvider
        where TQueryable : IQueryable
    {
        public delegate TQueryable ActivateQueryableWithExpressionDelegate(QueryProvider<TQueryable> queryProvider,
            Expression expression, Type elementType);

        private Func<QueryProvider<TQueryable>, Type, TQueryable> activateQueryable;
        private ActivateQueryableWithExpressionDelegate activateQueryableWithExpression;

        public QueryProvider(Func<QueryProvider<TQueryable>, Type, TQueryable> activateQueryable,
            ActivateQueryableWithExpressionDelegate activateQueryableWithExpression)
        {
            this.activateQueryable = activateQueryable;
            this.activateQueryableWithExpression = activateQueryableWithExpression;
        }

        public IQueryable CreateQuery(Expression expression)
        {
            var elementType = expression.Type.GetElementType();
            try
            {
                return activateQueryable(this, elementType);
            }
            catch (TargetInvocationException tie)
            {
                throw tie.InnerException;
            }
        }

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        {
            var queryable = this.activateQueryableWithExpression(this, expression, typeof(TElement));
            return queryable as IQueryable<TElement>;
        }

        public TResult Execute<TResult>(Expression expression)
        {
            return (TResult)this.Execute(expression);
        }

        public abstract object Execute(Expression expression);
    }
}

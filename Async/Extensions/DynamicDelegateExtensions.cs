using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace BlackBarLabs.Collections.Async
{
    public static class DynamicDelegateExtensions
    {
        public delegate TDelegate SandwichPreDelegate<TDelegate>();
        public delegate Task SandwichPostDelegate(Task callbackTask);

        public static async Task SandwichInvoke<TDelegate>(this YieldCallbackAsync<TDelegate> yieldAsync,
            SandwichPreDelegate<TDelegate> preCallback, SandwichPostDelegate postCallback)
        {
            var sandwichDelegate = GetSandwichDelegate<TDelegate>();
            await sandwichDelegate.Invoke(
                preCallback,
                yieldAsync,
                postCallback);
        }

        private delegate Task SandwichDelegate<TDelegate>(SandwichPreDelegate<TDelegate> preCallback,
            YieldCallbackAsync<TDelegate> yieldAsync, SandwichPostDelegate postCallback);

        //    SandwichDelegate<TDelegate> sandwichDelegate =
        //        (preCallback, yieldA, postCallback) =>
        //        {
        //            TDelegate invoked = (a, b, c) =>
        //            {
        //                var pre = preCallback.Invoke();
        //                var callbackTask = pre.Invoke(a, b, c);
        //                var delegateTask = postCallback.Invoke(callbackTask);
        //                return delegateTask;
        //            };
        //            var yieldTask = yieldAsync.Invoke(invoked);
        //            return yieldTask;
        //        };
        //    return sandwichDelegate;
        
        private static SandwichDelegate<TDelegate> GetSandwichDelegate<TDelegate>()
        {
            ParameterExpression preCallbackExpression, yieldAsyncExpression, postCallbackExpression;
            var delegateParameters = SandwichDelegateParameters<TDelegate>(
                out preCallbackExpression, out yieldAsyncExpression, out postCallbackExpression);

            // TDelegate invoked = (a, b, c) =>
            var invokedExpression = GetInvokedExpression<TDelegate>(
                preCallbackExpression, postCallbackExpression, delegateParameters);

            // var yieldTask = yieldAsync.Invoke(invoked);
            var yieldAsyncInvokeMethod = typeof(YieldCallbackAsync<TDelegate>).GetMethod("Invoke");
            var yieldAsyncInvokeCallExpression = Expression.Call(yieldAsyncExpression, yieldAsyncInvokeMethod,
                new Expression[] { invokedExpression });
            
            // return task
            var returnTarget = Expression.Label(typeof(Task));
            var returnTaskExpression = Expression.Return(returnTarget, yieldAsyncInvokeCallExpression);
            var returnValue = default(Task);
            var returnDefaultExpression = Expression.Constant(returnValue, typeof(Task));
            var returnExpression = Expression.Label(returnTarget, returnDefaultExpression);

            #region SandwichDelegate<TDelegate> sandwichDelegate = (preCallback, yieldA, postCallback) =>

            var sandwichDelegateParameters = new ParameterExpression[]
            {
                preCallbackExpression,
                yieldAsyncExpression,
                postCallbackExpression,
            };

            var sandwichDelegateBody = Expression.Block(new Expression[] {
                returnTaskExpression,
                returnExpression,
            });

            var sandwichDelegate = Expression.Lambda<SandwichDelegate<TDelegate>>(sandwichDelegateBody, sandwichDelegateParameters);

            #endregion

            var compiled = sandwichDelegate.Compile();
            return compiled;
        }

        private static ParameterExpression[] SandwichDelegateParameters<TDelegate>(
            out ParameterExpression preCallbackExpression,
            out ParameterExpression yieldAsyncExpression,
            out ParameterExpression postCallbackExpression)
        {
            var delegateInvokeMethod = typeof(TDelegate).GetMethod("Invoke");
            if (typeof(Task) != delegateInvokeMethod.ReturnType)
                throw new ArgumentException(
                    "Async Enumeration requires method that returns Task",
                    typeof(TDelegate).FullName);

            preCallbackExpression = Expression.Parameter(typeof(SandwichPreDelegate<TDelegate>), "preCallback");
            yieldAsyncExpression = Expression.Parameter(typeof(YieldCallbackAsync<TDelegate>), "yieldAsync");
            postCallbackExpression = Expression.Parameter(typeof(SandwichPostDelegate), "postCallback");

            var delegateParameters = delegateInvokeMethod.GetParameters()
                .Select(param => Expression.Parameter(param.ParameterType))
                .ToArray();
            return delegateParameters;
        }

        /// <summary>
        /// TDelegate invoked = (a, b, c) =>
        /// {
        ///    var pre = preCallback.Invoke();
        ///    var callbackTask = pre.Invoke(a, b, c);
        ///    var delegateTask = postCallback.Invoke(callbackTask);
        ///    return delegateTask;
        /// };
        /// </summary>
        /// <typeparam name="TDelegate"></typeparam>
        /// <param name="preCallbackExpression"></param>
        /// <param name="postCallbackExpression"></param>
        /// <param name="delegateParameters"></param>
        /// <returns></returns>
        private static Expression<TDelegate> GetInvokedExpression<TDelegate>(
            ParameterExpression preCallbackExpression,
            ParameterExpression postCallbackExpression,
            ParameterExpression[] delegateParameters)
        {
            // var pre = preCallback.Invoke();
            var preCallbackInvokeMethod = typeof(SandwichPreDelegate<TDelegate>).GetMethod("Invoke");
            var preExpression = Expression.Call(preCallbackExpression, preCallbackInvokeMethod, new Expression[] { });

            // var callbackTask = pre.Invoke(a, b, c);
            var delegateInvokeMethod = typeof(TDelegate).GetMethod("Invoke");
            var callbackTaskExpression = Expression.Call(preExpression, delegateInvokeMethod, delegateParameters);

            // var delegateTask = postCallback.Invoke(callbackTask);
            var postCallbackInvokeMethod = typeof(SandwichPostDelegate).GetMethod("Invoke");
            var delegateTaskExpression = Expression.Call(postCallbackExpression, postCallbackInvokeMethod,
                new Expression[] { callbackTaskExpression });

            // return delegateTask;
            var returnTarget = Expression.Label(typeof(Task));
            var returnTaskExpression = Expression.Return(returnTarget, delegateTaskExpression);
            var returnValue = default(Task);
            var returnLabelExpression = Expression.Label(returnTarget, Expression.Constant(returnValue, typeof(Task)));

            // TDelegate invoked = (a, b, c) =>
            var bodyExpression = new Expression[]
            {
                returnTaskExpression,
                returnLabelExpression,
            };
            var blockWithReturnExpression = Expression.Block(bodyExpression);
            var invokedExpression = Expression.Lambda<TDelegate>(blockWithReturnExpression, delegateParameters);
            return invokedExpression;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace BlackBarLabs.Collections.Async
{
    public delegate TDelegate ResultSwapPreDelegate<TDelegate>();
    public delegate Task ResultSwapPostDelegate<TResult>(TResult result);
    public delegate Task ResultSwapDelegate<TResult, TDelegateItems, TDelegateConvert>(
        ResultSwapPreDelegate<TDelegateConvert> preCallback,
        YieldCallbackAsync<TDelegateItems> yieldAsync,
        ResultSwapPostDelegate<TResult> postCallback);

    public static class DynamicDelegateResultSwapExtension
    {
        public static async Task ResultSwapInvoke<TResult, TDelegateItems, TDelegateConvert>(
            this YieldCallbackAsync<TDelegateItems> yieldAsync,
            ResultSwapPreDelegate<TDelegateConvert> preCallback,
            ResultSwapPostDelegate<TResult> postCallback)
        {
            var resultSwapDelegate = GetResultSwapDelegate<TResult, TDelegateItems, TDelegateConvert>();
            await resultSwapDelegate.Invoke(
                preCallback,
                yieldAsync,
                postCallback);
        }

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

        //ResultSwapDelegate<TestDelegateAsync, int, TestDelegateResult<int>> resultSwapDelegate =
        //    (preCallback, yieldA, postCallback) =>
        //    {
        //        TestDelegateAsync invoked = (a, b, c) =>
        //        {
        //            var pre = preCallback.Invoke();
        //            var preResult = pre.Invoke(a, b, c);
        //            var delegateTask = postCallback.Invoke(preResult);
        //            return delegateTask;
        //        };
        //        var yieldTask = yieldA.Invoke(invoked);
        //        return yieldTask;
        //    };
        //return resultSwapDelegate;

        private static ResultSwapDelegate<TResult, TDelegateItems, TDelegateConvert> GetResultSwapDelegate<TResult, TDelegateItems, TDelegateConvert>()
        {
            ParameterExpression preCallbackExpression, yieldAsyncExpression, postCallbackExpression;
            var delegateParameters = ResultSwapDelegateParameters<TResult, TDelegateItems, TDelegateConvert>(
                out preCallbackExpression, out yieldAsyncExpression, out postCallbackExpression);

            // TDelegate invoked = (a, b, c) =>
            var invokedExpression = GetInvokedExpression<TResult, TDelegateItems, TDelegateConvert>(
                preCallbackExpression, postCallbackExpression, delegateParameters);

            // var yieldTask = yieldAsync.Invoke(invoked);
            var yieldAsyncInvokeMethod = typeof(YieldCallbackAsync<TDelegateItems>).GetMethod("Invoke");
            var yieldAsyncInvokeCallExpression = Expression.Call(yieldAsyncExpression, yieldAsyncInvokeMethod,
                new Expression[] { invokedExpression });

            // return task
            var returnTarget = Expression.Label(typeof(Task));
            var returnTaskExpression = Expression.Return(returnTarget, yieldAsyncInvokeCallExpression);
            var returnValue = default(Task);
            var returnDefaultExpression = Expression.Constant(returnValue, typeof(Task));
            var returnExpression = Expression.Label(returnTarget, returnDefaultExpression);

            #region SandwichDelegate<TDelegate> sandwichDelegate = (preCallback, yieldA, postCallback) =>

            var resultSwapDelegateParameters = new ParameterExpression[]
            {
                preCallbackExpression,
                yieldAsyncExpression,
                postCallbackExpression,
            };

            var resultSwapDelegateBody = Expression.Block(new Expression[] {
                returnTaskExpression,
                returnExpression,
            });

            var resultSwapDelegate = Expression.Lambda<ResultSwapDelegate<TResult, TDelegateItems, TDelegateConvert>>(
                resultSwapDelegateBody, resultSwapDelegateParameters);

            #endregion

            var compiled = resultSwapDelegate.Compile();
            return compiled;
        }

        private static ParameterExpression[] ResultSwapDelegateParameters<TResult, TDelegateItems, TDelegateConvert>(
            out ParameterExpression preCallbackExpression,
            out ParameterExpression yieldAsyncExpression,
            out ParameterExpression postCallbackExpression)
        {
            var delegateInvokeMethod = typeof(TDelegateItems).GetMethod("Invoke");
            if (typeof(Task) != delegateInvokeMethod.ReturnType)
                throw new ArgumentException(
                    "Async Enumeration requires method that returns Task",
                    typeof(TDelegateItems).FullName);

            preCallbackExpression = Expression.Parameter(typeof(ResultSwapPreDelegate<TDelegateConvert>), "preCallback");
            yieldAsyncExpression = Expression.Parameter(typeof(YieldCallbackAsync<TDelegateItems>), "yieldAsync");
            postCallbackExpression = Expression.Parameter(typeof(ResultSwapPostDelegate<TResult>), "postCallback");

            var delegateParameters = delegateInvokeMethod.GetParameters()
                .Select(param => Expression.Parameter(param.ParameterType))
                .ToArray();
            return delegateParameters;
        }

        /// <summary>
        /// TestDelegateAsync invoked = (a, b, c) =>
        /// {
        ///     var pre = preCallback.Invoke();
        ///     var preResult = pre.Invoke(a, b, c);
        ///     var delegateTask = postCallback.Invoke(preResult);
        ///     return delegateTask;
        /// };
        /// </summary>
        /// <typeparam name="TDelegate"></typeparam>
        /// <param name="preCallbackExpression"></param>
        /// <param name="postCallbackExpression"></param>
        /// <param name="delegateParameters"></param>
        /// <returns></returns>
        private static Expression<TDelegateItems> GetInvokedExpression<TResult, TDelegateItems, TDelegateConvert>(
            ParameterExpression preCallbackExpression,
            ParameterExpression postCallbackExpression,
            ParameterExpression[] delegateParameters)
        {
            // var pre = preCallback.Invoke();
            var preCallbackInvokeMethod = typeof(ResultSwapPreDelegate<TDelegateConvert>).GetMethod("Invoke");
            var preExpression = Expression.Call(preCallbackExpression, preCallbackInvokeMethod, new Expression[] { });

            // var preResult = pre.Invoke(a, b, c);
            var delegateInvokeMethod = typeof(TDelegateConvert).GetMethod("Invoke");
            var callbackTaskExpression = Expression.Call(preExpression, delegateInvokeMethod, delegateParameters);

            // var delegateTask = postCallback.Invoke(preResult);
            var postCallbackInvokeMethod = typeof(ResultSwapPostDelegate<TResult>).GetMethod("Invoke");
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
            var invokedExpression = Expression.Lambda<TDelegateItems>(blockWithReturnExpression, delegateParameters);
            return invokedExpression;
        }
    }
}

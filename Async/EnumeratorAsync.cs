using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BlackBarLabs.Collections.Async
{
    internal delegate Task XmDelegate<TDelegate>(Barrier barrier, Func<TDelegate> totalCallbackCallback,
        EnumerableAsync.YieldCallbackAsync<TDelegate> yieldAsync, Action<Task> setCallbackTask);

    internal class EnumeratorAsync<TDelegate> : IEnumeratorAsync<TDelegate>
    {
        private Barrier callbackBarrier = new Barrier(2);

        private TDelegate totalCallback;
        private Task yieldAsyncTask;
        private Exception exception;
        internal Task callbackTask;

        internal XmDelegate<TDelegate> BuildXmDelegate()
        {
            var delegateInvokeMethod = typeof(TDelegate).GetMethod("Invoke");
            if (typeof(Task) != delegateInvokeMethod.ReturnType)
                throw new ArgumentException(
                    "Async Enumeration requires method that returns Task",
                    typeof(TDelegate).FullName);

            var delegateParameters = delegateInvokeMethod.GetParameters()
                .Select(param => Expression.Parameter(param.ParameterType))
                .ToArray();

            var barrierExpression = Expression.Parameter(typeof(Barrier), "barrier");
            var totalCallbackCallbackExpression = Expression.Parameter(typeof(Func<TDelegate>), "totalCallbackCallback");
            var yieldAsyncExpression = Expression.Parameter(typeof(EnumerableAsync.YieldCallbackAsync<TDelegate>), "yieldAsync");
            var setCallbackTaskExpression = Expression.Parameter(typeof(Action<Task>), "setCallbackTask");

            var signalAndWaitExpression1 = Expression.Call(barrierExpression,
                typeof(Barrier).GetMethod("SignalAndWait", new Type[] { }));
            var signalAndWaitExpression2 = Expression.Call(barrierExpression,
                 typeof(Barrier).GetMethod("SignalAndWait", new Type[] { }));
            
            // var tc = totalCallbackCallback.Invoke();
            var totalCallbackCallbackInvokeMethod = typeof(Func<TDelegate>).GetMethod("Invoke");
            var tcExpression = Expression.Call(totalCallbackCallbackExpression, totalCallbackCallbackInvokeMethod, new Expression[] { });
            
            // var callbackTask = tc.Invoke(a, b, c);
            var tcInvokeMethod = typeof(TDelegate).GetMethod("Invoke");
            var callbackTaskExpression = Expression.Call(tcExpression, tcInvokeMethod, delegateParameters);

            // setCallbackTask.Invoke(callbackTask);
            var setCallbackTaskInvokeMethod = typeof(Action<Task>).GetMethod("Invoke");
            var invokeSetCallbackTaskExpression = Expression.Call(setCallbackTaskExpression, setCallbackTaskInvokeMethod,
                new Expression[] { callbackTaskExpression } );

            // TestDelegateAsync invoked = (a, b, c) =>
            //var invokedExpression = Expression.Variable(typeof(TDelegate), "invoked");
            var meat = BuildWithOptionalTask(new Expression[]
            {
                signalAndWaitExpression1, // barrier.SignalAndWait();
                //tcAssignment, // var tc = totalCallbackCallback.Invoke();
                //callbackTaskAssignment, // var callbackTask = tc.Invoke(a, b, c);
                invokeSetCallbackTaskExpression, // setCallbackTask(callbackTask);
                signalAndWaitExpression2, // barrier.SignalAndWait();
            });
            var meatInvokeExpression = Expression.Lambda<TDelegate>(meat, delegateParameters);

            // var task = yieldAsync.Invoke(invoked);
            var yieldAsyncInvokeMethod = typeof(EnumerableAsync.YieldCallbackAsync<TDelegate>).GetMethod("Invoke");
            var yieldAsyncInvokeCallExpression = Expression.Call(yieldAsyncExpression, yieldAsyncInvokeMethod,
                new Expression[] { meatInvokeExpression });

            // return task
            var returnTarget = Expression.Label(typeof(Task));
            var returnValue = default(Task);
            var returnTaskExpression = Expression.Return(returnTarget, yieldAsyncInvokeCallExpression);
            var returnDefaultExpression = Expression.Constant(returnValue, typeof(Task));
            var returnExpression = Expression.Label(returnTarget, returnDefaultExpression);

            var xmParameters = new ParameterExpression[]
            {
                barrierExpression,
                totalCallbackCallbackExpression,
                yieldAsyncExpression,
                setCallbackTaskExpression
            };

            var xmBody = Expression.Block(new Expression[] {
                returnTaskExpression,
                returnExpression,
            });

            var lambda = Expression.Lambda<XmDelegate<TDelegate>>(xmBody, xmParameters);
            var compiled = lambda.Compile();
            return compiled;
        }

        private BlockExpression BuildWithOptionalTask(IEnumerable<Expression> meat)
        {
            var returnTarget = Expression.Label(typeof(Task));
            object returnValue = new Task(() => { });
            var taskExpression = Expression.Constant(returnValue);
            var returnTaskExpression = Expression.Return(returnTarget, taskExpression);

            var meatPlus = meat.Concat(new Expression[]
            {
                returnTaskExpression,
                Expression.Label(returnTarget, Expression.Constant(returnValue))
            });
            return Expression.Block(meatPlus);
        }

        //Func<Barrier, Func<TestDelegateAsync>, EnumerableAsync.YieldCallbackAsync<TestDelegateAsync>, Action<Task>, Task> xm =
        //        (barrier, totalCallbackCallback, yieldA, setCallbackTask) =>
        //        {
        //            TestDelegateAsync invoked = (a, b, c) =>
        //            {
        //                barrier.SignalAndWait();
        //                var tc = totalCallbackCallback.Invoke();
        //                var callbackTask = tc.Invoke(a, b, c);
        //                setCallbackTask.Invoke(callbackTask);
        //                barrier.SignalAndWait();
        //                return Task.FromResult(true);
        //            };
        //            var task = yieldA.Invoke(invoked);
        //            return task;
        //        };
        //    return xm;

        internal EnumeratorAsync(EnumerableAsync.YieldCallbackAsync<TDelegate> yieldAsync)
        {
            yieldAsyncTask = Task.Run(async () =>
            {
                XmDelegate<TDelegate> xm;
                try
                {
                    xm = BuildXmDelegate();
                } catch(Exception ex)
                {
                    exception = ex;
                    if (callbackBarrier.ParticipantCount == 2)
                        callbackBarrier.RemoveParticipant();
                    throw ex;
                }
                await xm.Invoke(
                    callbackBarrier,
                    () => this.totalCallback,
                    yieldAsync,
                    (updatedCallbackTask) => { callbackTask = updatedCallbackTask; });
                callbackBarrier.RemoveParticipant();
            });
        }
        
        #region IEnumeratorAsync

        public async Task<bool> MoveNextAsync(TDelegate callback)
        {
            totalCallback = callback;
            callbackBarrier.SignalAndWait();
            if (default(Exception) != exception)
                throw exception;
            if (yieldAsyncTask.IsCompleted)
                return false;
            callbackBarrier.SignalAndWait();
            await callbackTask;
            return true;
        }

        public Task ResetAsync()
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}

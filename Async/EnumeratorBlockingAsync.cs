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
    internal class EnumeratorBlockingAsync<TDelegate> : IEnumeratorAsync<TDelegate>, IDisposable
    {
        private Barrier callbackBarrier = new Barrier(2);

        private TDelegate totalCallback;
        private Task yieldAsyncTask;
        private bool complete = false;
        private Exception exception;
        internal Task callbackTask;
        private CancellationTokenSource cancelYieldAsyncTokenSource = new CancellationTokenSource();

        internal EnumeratorBlockingAsync(YieldCallbackAsync<TDelegate> yieldAsync)
        {
            var cancelYieldAsyncToken = cancelYieldAsyncTokenSource.Token;
            yieldAsyncTask = Task.Run(async () =>
            {
                try
                {
                    // TODO: Get cancellation token on this thread as well.
                    await yieldAsync.SandwichInvoke(
                        () =>
                        {
                            callbackBarrier.SignalAndWait();
                            return this.totalCallback;
                        },
                        (updatedCallbackTask) =>
                        {
                            callbackTask = updatedCallbackTask;
                            callbackBarrier.SignalAndWait();
                            return Task.FromResult(true);
                        });
                } catch(Exception ex)
                {
                    exception = ex;
                    throw ex;
                } finally
                {
                    complete = true;
                    callbackBarrier.RemoveParticipant();
                }
            }, cancelYieldAsyncToken);
        }
        
        #region IEnumeratorAsync

        public async Task<bool> MoveNextAsync(TDelegate callback)
        {
            totalCallback = callback;
            callbackBarrier.SignalAndWait();
            if (default(Exception) != exception)
                throw exception;
            if (this.complete)
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
        
        public void Dispose()
        {
            cancelYieldAsyncTokenSource.Cancel();
        }

    }
}

using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;

namespace BlackBarLabs.Collections.Async
{
    public class EnumerableNonAsync<TResult, TDelegateItems, TDelegateConvert> : IEnumerable<TResult>
    {
        private TDelegateConvert convertDelegate;
        private YieldCallbackAsync<TDelegateItems> yieldAsync;

        public EnumerableNonAsync(YieldCallbackAsync<TDelegateItems> yieldAsync, TDelegateConvert convertDelegate)
        {
            this.yieldAsync = yieldAsync;
            this.convertDelegate = convertDelegate;
        }

        public IEnumerator<TResult> GetEnumerator()
        {
            return new EnumeratorNonAsync<TResult, TDelegateItems, TDelegateConvert>(this.yieldAsync, this.convertDelegate);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new EnumeratorNonAsync<TResult, TDelegateItems, TDelegateConvert>(this.yieldAsync, this.convertDelegate);
        }
    }
}

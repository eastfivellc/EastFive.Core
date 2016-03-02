using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlackBarLabs.Collections.Async
{
    public delegate Task YieldCallbackAsync<TDelegate>(TDelegate yield);

    public class EnumerableAsync
    {
        public static IEnumerableAsync<TDelegate> YieldAsync<TDelegate>(YieldCallbackAsync<TDelegate> yieldAsync)
        {
            return new EnumerableAsync<TDelegate>(yieldAsync);
        }
    }

    internal class EnumerableAsync<TDelegate> : IEnumerableAsync<TDelegate>
    {
        private YieldCallbackAsync<TDelegate> yieldAsync;

        public EnumerableAsync(YieldCallbackAsync<TDelegate> yieldAsync)
        {
            this.yieldAsync = yieldAsync;
        }

        public IEnumerable<TResult> GetEnumerable<TResult, TDelegateConvert>(TDelegateConvert convertDelegate)
        {
            return new EnumerableNonAsync<TResult, TDelegate, TDelegateConvert>(this.yieldAsync, convertDelegate);
        }

        public IEnumeratorAsync<TDelegate> GetEnumerator()
        {
            return new EnumeratorBlockingAsync<TDelegate>(this.yieldAsync);
        }

        public IIteratorAsync<TDelegate> GetIterator()
        {
            return new IteratorSimpleAsync<TDelegate>(this.yieldAsync);
        }
    }
}

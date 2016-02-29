using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlackBarLabs.Collections.Async
{
    public class EnumerableAsync
    {
        public delegate Task YieldCallbackAsync<TDelegate>(TDelegate yield);
        public static IEnumerableAsync<TDelegate> YieldAsync<TDelegate>(YieldCallbackAsync<TDelegate> yieldAsync)
        {
            return new EnumerableAsync<TDelegate>(yieldAsync);
        }

    }

    internal class EnumerableAsync<TDelegate> : IEnumerableAsync<TDelegate>
    {
        private EnumerableAsync.YieldCallbackAsync<TDelegate> yieldAsync;

        public EnumerableAsync(EnumerableAsync.YieldCallbackAsync<TDelegate> yieldAsync)
        {
            this.yieldAsync = yieldAsync;
        }

        public IEnumeratorAsync<TDelegate> GetEnumerator()
        {
            return new EnumeratorAsync<TDelegate>(this.yieldAsync);
        }
    }
}

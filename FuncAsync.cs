using BlackBarLabs.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlackBarLabs.Core
{
    public delegate Task<TResult> FuncRollback<TResult>(Func<Func<Task>, Task<TResult>> onSuccess);

    public static class FuncAsyncExtensions
    {
        public static Task<TResult> ExecuteAsync<TResult>(this IEnumerable<FuncRollback<TResult>> tasks, Func<TResult> onSuccess)
        {
            var enumerable = tasks.GetEnumerator();
            return enumerable.ExecuteAsync(onSuccess,
                (t) => t.ToTask(),
                (t) => t.ToTask());
        }

        private static async Task<TResult> ExecuteAsync<TResult>(this IEnumerator<FuncRollback<TResult>> tasks, Func<TResult> onFinalSuccess,
            Func<TResult, Task<TResult>> onSuccess,
            Func<TResult, Task<TResult>> onFailure)
        {
            if (!tasks.MoveNext())
                return onFinalSuccess();

            var task = tasks.Current;
            return await task(
                (rollback) =>
                {
                    return tasks.ExecuteAsync(onFinalSuccess,
                        (t) => t.ToTask(),
                        async (t) =>
                        {
                            await rollback();
                            return t;
                        });
                });
        }
    }
}

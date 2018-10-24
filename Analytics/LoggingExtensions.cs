using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EastFive.Extensions;
using Microsoft.Extensions.Logging;

namespace EastFive.Analytics
{
    public static class LoggingExtensions
    {
        public interface IScope<TResult>
        {
            TResult Result { get; }
        }

        private class Disposable<TResult> : IScope<TResult>, IDisposable
        {
            public TResult result;
            internal Disposable(TResult result)
            {
                this.result = result;
            }

            public TResult Result => result;

            public void Dispose()
            {
            }
        }

        public static TResult Scope<TState, TResult>(this ILogger logger, TState state,
            Func<Func<TResult, IScope<TResult>>, IScope<TResult>> onScoped = 
                default(Func<Func<TResult, IScope<TResult>>, IScope<TResult>>))
        {
            var disposal = logger.IsDefault()?
                default(IDisposable)
                :
                logger.BeginScope<TState>(state);
            
            return onScoped(
                (result) =>
                {
                    if (!disposal.IsDefaultOrNull())
                        disposal.Dispose();
                    return new Disposable<TResult>(result);
                }).Result;
        }

        public static IDisposable CreateScope<TState>(this ILogger logger, TState state)
        {
            if (logger.IsDefault())
                return new Disposable<TState>(state);
            return logger.BeginScope(state);
        }

        public static void Information(this ILogger logger, string message)
        {
            if (logger.IsDefault())
                return;
            logger.LogInformation(message);
        }

        public static void Trace(this ILogger logger, string message)
        {
            if (logger.IsDefault())
                return;
            logger.LogTrace(message);
        }
    }
}

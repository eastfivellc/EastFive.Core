using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EastFive.Functional
{
    public static class DiscriminatedFunctions
    {
        public static Func<Func<TStep, TResult>, TResult> AsDiscriminatedStep<TStep, TResult>(this TStep stepValue)
        {
            Func<Func<TStep, TResult>, TResult> callback = 
                (nextStep) =>
                {
                    return nextStep(stepValue);
                };

            return callback;
        }

        public static Func<Func<TStep, TResult>, TResult> AsDiscriminatedResult<TStep, TResult>(this Func<TResult> stepValue)
        {
            Func<Func<TStep, TResult>, TResult> callback =
                (nextStep) =>
                {
                    return stepValue();
                };

            return callback;
        }
    }
}

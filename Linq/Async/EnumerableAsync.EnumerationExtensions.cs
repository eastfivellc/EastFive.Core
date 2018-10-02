using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EastFive.Linq.Async
{
    public static partial class EnumerableAsync
    {
        public static Task<bool> AnyAsync<T>(this IEnumerableAsync<T> enumerable)
        {
            var enumerator = enumerable.GetEnumerator();
            return enumerator.MoveNextAsync();
        }

        public static async Task<IEnumerable<T>> Async<T>(this IEnumerableAsync<T> enumerable)
        {
            var enumerator = enumerable.GetEnumerator();
            var firstStep = new Step<T>
            {
                current = default(T),
                steps = new Step<T>?[2],
            };
            var step = firstStep;
            while (await enumerator.MoveNextAsync())
            {
                var nextStep = new Step<T>
                {
                    current = enumerator.Current,
                    steps = new Step<T>?[2],
                };
                nextStep.steps[StepEnumerable<T>.StepEnumerator.lastIndex] = step;
                step.steps[StepEnumerable<T>.StepEnumerator.nextIndex] = nextStep;
                step = nextStep;
            }
            return new StepEnumerable<T>(firstStep);
        }
        
        public static async Task<T[]> ToArrayAsync<T>(this IEnumerableAsync<T> enumerableAsync)
        {
            var enumerable = await enumerableAsync.Async();
            return enumerable.ToArray();
        }

        private struct Step<T>
        {
            public Step<T>?[] steps;
            public T current;
        }

        private class StepEnumerable<T> : IEnumerable<T>
        {
            internal class StepEnumerator : IEnumerator<T>
            {
                internal const int lastIndex = 0;
                internal const int nextIndex = 1;

                protected Step<T> current;
                public StepEnumerator(Step<T> current)
                {
                    this.current = current;
                }

                public T Current => current.current;

                object IEnumerator.Current => current.current;

                public void Dispose()
                {
                }

                public bool MoveNext()
                {
                    if (!current.steps[nextIndex].HasValue)
                        return false;
                    current = current.steps[nextIndex].Value;
                    return true;
                }

                public void Reset()
                {
                    while (current.steps[lastIndex].HasValue)
                        current = current.steps[lastIndex].Value;
                }
            }
            
            private Step<T> firstStep;
            public StepEnumerable(Step<T> firstStep)
            {
                this.firstStep = firstStep;
            }

            public IEnumerator<T> GetEnumerator()
            {
                return new StepEnumerator(firstStep);
            }

            IEnumerator IEnumerable.GetEnumerator() =>
                this.GetEnumerator();
        }
        
        
    }
}

using BlackBarLabs.Extensions;
using EastFive.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EastFive.Linq.Async
{
    public interface IYieldResult<T>
    {
        T Value { get; }

        Task<bool> HasNext(
            Func<IYieldResult<T>, bool> onMore,
            Func<bool> onEnd);
    }

    public delegate Task<IYieldResult<T>> YieldDelegateAsync<T>(
            Func<T, IYieldResult<T>> yieldReturn,
            IYieldResult<T> yieldBreak);

    public static partial class EnumerableAsync
    {
        private struct YieldEnumerable<T> : IEnumerableAsync<T>
        {
            private class YieldResult : IYieldResult<T>
            {
                private T value;
                private YieldDelegateAsync<T> getNext;
                private Task<IYieldResult<T>> fetch;
                private bool hasFeched;

                internal YieldResult(T value, YieldDelegateAsync<T> next)
                {
                    this.value = value;
                    this.getNext = next;
                    this.hasFeched = false;
                }

                public virtual T Value => value;

                public async Task<bool> HasNext(Func<IYieldResult<T>, bool> onMore, Func<bool> onEnd)
                {
                    Task<IYieldResult<T>> internalFetch;
                    lock(this)
                    {
                        if (!this.hasFeched)
                        {
                            this.fetch = getNext(
                                v =>
                                {
                                    return new YieldResult(v, this.getNext);
                                },
                                new YieldBreak());
                            this.hasFeched = true;
                        }
                        internalFetch = this.fetch;
                    }

                    try
                    {
                        var yieldResult = await internalFetch;
                        var isTerminal = (yieldResult is YieldBreak);
                        if (isTerminal)
                            return onEnd();
                        return onMore(yieldResult);
                    }catch (Exception ex)
                    {
                        throw ex;
                    }
                }

                private struct YieldBreak : IYieldResult<T>
                {
                    public T Value => throw new NullReferenceException();

                    public Task<bool> HasNext(Func<IYieldResult<T>, bool> onMore, Func<bool> onEnd)
                    {
                        return onEnd().ToTask();
                    }
                }

            }

            private class YieldResultFirst : YieldResult
            {
                internal YieldResultFirst(YieldDelegateAsync<T> next)
                    : base(default(T), next)
                {
                }

                public override T Value => throw new NullReferenceException();
                
            }

            // private YieldDelegateAsync<T> yield;
            private IYieldResult<T> firstStep;

            internal YieldEnumerable(YieldDelegateAsync<T> yield)
            {
                this.firstStep = new YieldResultFirst(yield);
            }
            
            private class YieldEnumerator : IEnumeratorAsync<T>
            {
                private IYieldResult<T> currentStep;
                public YieldEnumerator(IYieldResult<T> firstStep)
                {
                    this.currentStep = firstStep;
                }

                public T Current => currentStep.Value;

                public Task<bool> MoveNextAsync()
                {
                    return this.currentStep.HasNext(
                        (next) =>
                        {
                            this.currentStep = next;
                            return true;
                        },
                        () =>
                        {
                            //this.currentStep = null;
                            return false;
                        });
                }
            }

            public IEnumeratorAsync<T> GetEnumerator()
            {
                return new YieldEnumerator(firstStep);
            }
        }

        public static IEnumerableAsync<T> Yield<T>(
            YieldDelegateAsync<T> generateFunction)
        {
            return new YieldEnumerable<T>(generateFunction);
        }

        private struct YieldResultBatch<TItem> : IYieldResult<TItem[]>
        {
            public YieldResultBatch(TItem [] value)
            {
                this.Value = value;
            }

            public TItem[] Value  {get; private set;}

            public Task<bool> HasNext(Func<IYieldResult<TItem[]>, bool> onMore, Func<bool> onEnd)
            {
                throw new NotImplementedException();
            }
        }

        private struct YieldBreakBatch<TItem> : IYieldResult<TItem[]>
        {
            public TItem[] Value { get; private set; }

            public Task<bool> HasNext(Func<IYieldResult<TItem[]>, bool> onMore, Func<bool> onEnd)
            {
                throw new NotImplementedException();
            }
        }

        public static IEnumerableAsync<T> YieldBatch<T>(
            YieldDelegateAsync<T[]> generateFunction)
        {
            var index = 0;
            var segment = new T[] { };
            var yieldBreakSegment = new YieldBreakBatch<T>();
            return Yield<T>(
                async (yieldReturn, yieldBreak) =>
                {
                    if (segment.Length <= index)
                    {
                        var yieldResult = await generateFunction(
                            (nextSegment) =>
                            {
                                return new YieldResultBatch<T>(nextSegment);
                            },
                            yieldBreakSegment);
                        if (yieldResult == yieldBreak)
                            return yieldBreak;
                        segment = yieldResult.Value;
                        index = 0;
                    }
                    var value = segment[index];
                    index++;
                    return yieldReturn(value);
                });
        }

        public static IEnumerableAsync<T> Range<T>(int start, int count,
            Func<int, Task<T>> generateFunction)
        {
            return Yield<T>(
                async (yieldReturn, yieldBreak) =>
                {
                    if (count == 0)
                        return yieldBreak;
                    count--;
                    var index = start;
                    start++;
                    var value = await generateFunction(index);
                    return yieldReturn(value);
                });
        }

        public static IEnumerableAsync<T> From<T>(params T[] items)
        {
            var count = -1;
            return Yield<T>(
                async (yieldReturn, yieldBreak) =>
                {
                    count++;
                    if (items.Length == count)
                        return yieldBreak;

                    var item = items[count];
                    return yieldReturn(item);
                });
        }

        public static IEnumerableAsync<TItem> AsyncEnumerable<TItem>(this IEnumerable<Task<TItem>> items)
        {
            var enumerator = items.GetEnumerator();
            return Yield<TItem>(
                async (yieldReturn, yieldBreak) =>
                {
                    if (!enumerator.MoveNext())
                        return yieldBreak;

                    var current = await enumerator.Current;
                    return yieldReturn(current);
                });
        }
    }
}

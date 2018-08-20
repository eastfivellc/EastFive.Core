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
        public static IEnumerableAsync<T> Where<T>(this IEnumerableAsync<T> enumerable, Func<T, bool> predicate)
        {
            return new DelegateEnumerableAsync<T, T>(enumerable,
                async (enumeratorAsync, enumeratorDestination, moved, ended) =>
                {
                    if (!await enumeratorAsync.MoveNextAsync())
                        return ended();
                    var current = enumeratorAsync.Current;
                    while (!predicate(current))
                    {
                        if (!await enumeratorAsync.MoveNextAsync())
                            return ended();
                        current = enumeratorAsync.Current;
                    }
                    return moved(current);
                });
        }

        public static IEnumerableAsync<TResult> Select<T, TResult>(this IEnumerableAsync<T> enumerable, Func<T, TResult> selection)
        {
            return new DelegateEnumerableAsync<TResult, T>(enumerable,
                async (enumeratorAsync, enumeratorDestination, moved, ended) =>
                {
                    if (!await enumeratorAsync.MoveNextAsync())
                        return ended();
                    var current = enumeratorAsync.Current;
                    var next = selection(current);
                    return moved(next);
                });
        }


        public static Task<TResult> FirstAsync<T, TResult>(this IEnumerableAsync<T> enumerable,
            Func<T, Func<Task<TResult>>, Task<TResult>> onOne,
            Func<TResult> onNone)
        {
            var enumerator = enumerable.GetEnumerator();
            return FirstInnerAsync(enumerator, onOne, onNone);
        }

        private static async Task<TResult> FirstInnerAsync<T, TResult>(IEnumeratorAsync<T> enumerator,
            Func<T, Func<Task<TResult>>, Task<TResult>> onOne,
            Func<TResult> onNone)
        {
            while (await enumerator.MoveNextAsync())
            {
                return await onOne(enumerator.Current,
                    () => FirstInnerAsync(enumerator, onOne, onNone));
            }
            return onNone();
        }


        public static IEnumerableAsync<T> Take<T>(this IEnumerableAsync<T> enumerable, int count)
        {
            return new AppendedDelegateEnumerableAsync<T, T, int>(count, enumerable,
                async (countInner, enumeratorAsync, moved, ended) =>
                {
                    if (countInner <= 0)
                        return ended(countInner);
                    countInner--;
                    if (!await enumeratorAsync.MoveNextAsync())
                        return ended(countInner);
                    return moved(enumeratorAsync.Current, countInner);
                });
        }

        public static IEnumerableAsync<T> Skip<T>(this IEnumerableAsync<T> enumerable, int count)
        {
            return new AppendedDelegateEnumerableAsync<T, T, int>(count, enumerable,
                async (countInner, enumeratorAsync, moved, ended) =>
                {
                    if (!await enumeratorAsync.MoveNextAsync())
                        return ended(countInner);
                    while (countInner > 0)
                    {
                        countInner--;
                        if (!await enumeratorAsync.MoveNextAsync())
                            return ended(countInner);
                    }
                    return moved(enumeratorAsync.Current, countInner);
                });
        }

        public static IEnumerableAsync<T> Distinct<T>(this IEnumerableAsync<T> enumerable)
        {
            var accumulation = new T[] { }; // TODO: Should be a hash
            return new DelegateEnumerableAsync<T, T>(enumerable,
                async (enumeratorAsync, enumeratorDestination, moved, ended) =>
                {
                    if (!await enumeratorAsync.MoveNextAsync())
                        return ended();
                    var current = enumeratorAsync.Current;
                    while (accumulation.Contains(current))
                    {
                        if (!await enumeratorAsync.MoveNextAsync())
                            return ended();
                        current = enumeratorAsync.Current;
                    }
                    accumulation = accumulation.Append(current).ToArray();
                    return moved(current);
                });
        }
        
        public static IEnumerableAsync<T> Await<T>(this IEnumerableAsync<Task<T>> enumerable)
        {
            return new DelegateEnumerableAsync<T, Task<T>>(enumerable,
                async (enumeratorAsync, enumeratorDestination, moved, ended) =>
                {
                    if (!await enumeratorAsync.MoveNextAsync())
                        return ended();
                    var next = await enumeratorAsync.Current;
                    return moved(next);
                });
        }

        private abstract class LinqEnumerableAsync<T, TSource> : IEnumerableAsync<T>
        {
            protected IEnumerableAsync<TSource> enumerableAsync;

            public virtual IEnumeratorAsync<T> GetEnumerator()
            {
                return new LinqEnumeratorAsync(this, enumerableAsync.GetEnumerator());
            }

            internal LinqEnumerableAsync(IEnumerableAsync<TSource> enumerableAsync)
            {
                this.enumerableAsync = enumerableAsync;
            }

            protected abstract Task<TResult> MoveNextAsync<TResult>(IEnumeratorAsync<TSource> enumeratorAsync, IEnumeratorAsync<T> enumeratorDestination,
                Func<T, TResult> moved,
                Func<TResult> ended);

            protected internal class LinqEnumeratorAsync : IEnumeratorAsync<T>
            {
                private IEnumeratorAsync<TSource> enumeratorAsync;
                private LinqEnumerableAsync<T, TSource> enumerableAsync;

                internal LinqEnumeratorAsync(LinqEnumerableAsync<T, TSource> enumerableAsync, IEnumeratorAsync<TSource> enumeratorAsync)
                {
                    this.enumerableAsync = enumerableAsync;
                    this.enumeratorAsync = enumeratorAsync;
                }

                public T Current { get; protected set; }

                public Task<bool> MoveNextAsync()
                {
                    return this.enumerableAsync.MoveNextAsync(enumeratorAsync, this,
                        (c) =>
                        {
                            this.Current = c;
                            return true;
                        },
                        () => false);
                }
            }
        }

        private class DelegateEnumerableAsync<T, TSource> : LinqEnumerableAsync<T, TSource>
        {
            internal Func<IEnumeratorAsync<TSource>, IEnumeratorAsync<T>, Func<T, object>, Func<object>, Task<object>> MoveNext { private get; set; }

            public DelegateEnumerableAsync(IEnumerableAsync<TSource> enumerableAsync,
                Func<IEnumeratorAsync<TSource>, IEnumeratorAsync<T>, Func<T, object>, Func<object>, Task<object>> moveNext)
                : base(enumerableAsync)
            {
                this.MoveNext = moveNext;
            }

            protected async override Task<TResult> MoveNextAsync<TResult>(IEnumeratorAsync<TSource> enumeratorAsync, IEnumeratorAsync<T> enumeratorDestination, Func<T, TResult> moved, Func<TResult> ended)
            {
                return (TResult)(await MoveNext(enumeratorAsync, enumeratorDestination, (x) => moved(x), () => ended()));
            }
        }

        /// <summary>
        /// Sometimes there are paramters that have state that is carried throught the enumeration. In that case,
        /// an appended variable needs to be carried via the Enumerator. This class serves that purpose.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TSource"></typeparam>
        /// <typeparam name="TAppend"></typeparam>
        private class AppendedDelegateEnumerableAsync<T, TSource, TAppend> : DelegateEnumerableAsync<T, TSource>
        {
            public TAppend Appendage;
            public delegate Task<object> CallbackDelegate(TAppend Appendage, IEnumeratorAsync<TSource> enumerator,
                Func<T, TAppend, object> moved,
                Func<TAppend, object> ended);

            public AppendedDelegateEnumerableAsync(TAppend appendage, IEnumerableAsync<TSource> enumerableAsync,
                CallbackDelegate moveNext)
                :
                    base(enumerableAsync,
                        (enumeratorAsync, enumeratorDestination, moved, ended) =>
                        {
                            if (!(enumeratorDestination is AppendedDelegateEnumeratorAsync))
                                throw new Exception();

                            var cde = enumeratorDestination as AppendedDelegateEnumeratorAsync;
                            return moveNext(cde.Appendage, enumeratorAsync,
                                (r, next) =>
                                {
                                    cde.Appendage = next;
                                    return moved(r);
                                },
                                (next) =>
                                {
                                    cde.Appendage = next;
                                    return ended();
                                });
                        })
            {
                this.Appendage = appendage;
            }

            public override IEnumeratorAsync<T> GetEnumerator()
            {
                return new AppendedDelegateEnumeratorAsync(Appendage, this, enumerableAsync.GetEnumerator());
            }

            private class AppendedDelegateEnumeratorAsync : LinqEnumeratorAsync
            {
                public TAppend Appendage;

                internal AppendedDelegateEnumeratorAsync(TAppend appendage, LinqEnumerableAsync<T, TSource> enumerableAsync, IEnumeratorAsync<TSource> enumeratorAsync)
                    : base(enumerableAsync, enumeratorAsync)
                {
                    this.Appendage = appendage;
                }
            }
        }
    }
}

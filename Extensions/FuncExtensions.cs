using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using BlackBarLabs.Extensions;

namespace EastFive
{
    public static class FuncExtensions
    {
        [DebuggerStepThrough]
        [DebuggerNonUserCode]
        [DebuggerStepperBoundary]
        public static Func<Task<T>> AsAsyncFunc<T>(this Func<T> value)
        {
            return () => value().ToTask();
        }

        [DebuggerStepThrough]
        [DebuggerNonUserCode]
        [DebuggerStepperBoundary]
        public static Func<T1, Task<T>> AsAsyncFunc<T, T1>(this Func<T1, T> value)
        {
            return (v1) => value(v1).ToTask();
        }

        [DebuggerStepThrough]
        [DebuggerNonUserCode]
        [DebuggerStepperBoundary]
        public static Func<T1, Task<T>> AsAsyncFunc<T, T1>(this Func<T> value)
        {
            return (value1) => value().ToTask();
        }

        [DebuggerStepThrough]
        [DebuggerNonUserCode]
        [DebuggerStepperBoundary]
        public static Func<T1, T2, Task<T>> AsAsyncFunc<T, T1, T2>(this Func<T1, T2, T> value)
        {
            return (v1, v2) => value(v1, v2).ToTask();
        }
    }
}

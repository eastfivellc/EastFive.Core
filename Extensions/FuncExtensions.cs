using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using BlackBarLabs.Extensions;
using EastFive.Extensions;

namespace EastFive
{
    public class ResultCaseNotHandledException : Exception
    {
        public ResultCaseNotHandledException(string caseName, ArgumentNullException innerException)
            : base($"Unhandled result case `{caseName}`", innerException)
        {
            this.CaseName = caseName;
        }

        public string CaseName { get; set; }
    }

    public static class FuncExtensions
    {
        [DebuggerStepThrough]
        [DebuggerNonUserCode]
        [DebuggerStepperBoundary]
        public static Func<Task<T>> AsAsyncFunc<T>(this Func<T> value)
        {
            return () => value.InvokeNotDefault().ToTask();
        }

        public static T InvokeNotDefault<T>(this Func<T> func)
        {
            if (!func.IsDefault())
                return func();

            try
            {
                return func.Invoke();
            } catch(ArgumentNullException ex)
            {
                var st = ex.StackTrace;
                var resultCaseEx = new ResultCaseNotHandledException(st, ex);
                resultCaseEx.CaseName = st;
                throw resultCaseEx;
            }
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

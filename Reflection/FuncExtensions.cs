using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EastFive.Reflection
{
    public static class FuncExtensions
    {
        public static object MakeDelegate<TResult>(this Func<TResult> func, Type delegateType)
        {
            var delegateGeneric = Delegate.CreateDelegate(delegateType,
                func.Target, func.Method);
            return delegateGeneric;
        }

        public static object MakeDelegate<T1, TResult>(this Func<T1, TResult> func, Type delegateType)
        {
            var delegateGeneric = Delegate.CreateDelegate(delegateType,
                func.Target, func.Method);
            return delegateGeneric;
        }

        public static object MakeDelegate<T1, T2, TResult>(this Func<T1, T2, TResult> func, Type delegateType)
        {
            var delegateGeneric = Delegate.CreateDelegate(delegateType,
                func.Target, func.Method);
            return delegateGeneric;
        }

        public static object MakeDelegate<T1, T2, T3, TResult>(this Func<T1, T2, T3, TResult> func, Type delegateType)
        {
            var delegateGeneric = Delegate.CreateDelegate(delegateType,
                func.Target, func.Method);
            return delegateGeneric;
        }

        public static object MakeDelegate<T1, T2, T3, T4, TResult>(this Func<T1, T2, T3, T4, TResult> func, Type delegateType)
        {
            var delegateGeneric = Delegate.CreateDelegate(delegateType,
                func.Target, func.Method);
            return delegateGeneric;
        }

        public static object MakeDelegate<T1, T2, T3, T4, T5, TResult>(this Func<T1, T2, T3, T4, T5, TResult> func, Type delegateType)
        {
            var delegateGeneric = Delegate.CreateDelegate(delegateType,
                func.Target, func.Method);
            return delegateGeneric;
        }

        public static object MakeDelegate<T1, T2, T3, T4, T5, T6, TResult>(this Func<T1, T2, T3, T4, T5, T6, TResult> func, Type delegateType)
        {
            var delegateGeneric = Delegate.CreateDelegate(delegateType,
                func.Target, func.Method);
            return delegateGeneric;
        }

        public static TDelegate MakeDelegate<TDelegate, TResult>(this Func<TResult> func)
            where TDelegate : MulticastDelegate
        {
            var delegateGeneric = Delegate.CreateDelegate(typeof(TDelegate),
                func.Target, func.Method);
            var delegateCast = delegateGeneric as TDelegate;
            return delegateCast;
        }
    }
}

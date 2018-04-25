using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EastFive
{
    public static class ConditionalExtensions
    {
        public static TResult IfElse<TResult>(this bool v, Func<TResult> ifFunc, Func<TResult> elseFunc)
        {
            if (v)
                return ifFunc();
            return elseFunc();
        }
    }
}

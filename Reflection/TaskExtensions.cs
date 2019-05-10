using EastFive.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace EastFive.Reflection
{
    public static class TaskExtensions
    {
        public static Task<T> CastTask<T>(this object taskObj)
        {
            var taskType = taskObj.GetType();
            if (!taskType.IsSubClassOfGeneric(typeof(Task<>)))
                throw new ArgumentException($"{taskType.FullName} is not of type Task<>");
            var resultType = taskType.GenericTypeArguments.First();
            var castTaskMethodGeneric = typeof(TaskExtensions).GetMethod("CastTaskInner", BindingFlags.Static | BindingFlags.Public);
            var castTaskMethod = castTaskMethodGeneric.MakeGenericMethod(new Type[] { resultType, typeof(T) });
            var objCastTask = castTaskMethod.Invoke(null, new object[] { taskObj });
            return objCastTask as Task<T>;
        }

        public static async Task<TResult> CastTaskInner<T, TResult>(Task<T> task)
        {
            var t = await task;
            var tObj = (object)t;
            return (TResult)tObj;
        }

    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlackBarLabs
{
    public static class StringExtensions
    {
        public static TResult TrimEnd<TResult>(this string str, string trim,
            Func<string, TResult> success, Func<string, TResult> doesNotEndWithTrim)
        {
            if (!str.EndsWith(trim))
                return doesNotEndWithTrim(str);
            var trimmed = str.Substring(0, str.Length - trim.Length);
            return success(trimmed);
        }
    }
}

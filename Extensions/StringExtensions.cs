using System;
using System.Collections.Generic;
using System.IO;
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

        public static string Format(this string str, params object [] args)
        {
            return String.Format(str, args);
        }

        public static string Join(this IEnumerable<string> strings, string separator)
        {
            return String.Join(separator, strings);
        }

        public static Type GetClrType(this string type)
        {
            if (type.ToLower() == "string")
                return typeof(string);
            if (type.ToLower() == "int")
                return typeof(int);
            if (type.ToLower() == "decimal")
                return typeof(decimal);
            if (type.ToLower() == "double")
                return typeof(double);
            if (type.ToLower() == "long")
                return typeof(long);
            if (type.ToLower() == "single")
                return typeof(float);
            if (type.ToLower() == "int32")
                return typeof(int);
            throw new InvalidDataException($"Type {type} not supported");
        }
    }
}

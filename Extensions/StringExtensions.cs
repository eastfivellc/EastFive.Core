using System;
using System.Collections.Generic;
using System.IO;
using EastFive.Linq;

namespace EastFive
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

        public static string Format(this string str, params object[] args)
        {
            return String.Format(str, args);
        }

        public static string Join(this IEnumerable<string> strings, string separator)
        {
            return String.Join(separator, strings.NullToEmpty());
        }

        public static string Join(this IEnumerable<string> strings, char separator)
        {
            return String.Join(separator.ToString(), strings.NullToEmpty());
        }

        public static string Base64(this string value, System.Text.Encoding encoding = default(System.Text.Encoding))
        {
            if (default(System.Text.Encoding) == encoding)
                encoding = System.Text.Encoding.UTF8;
            var bytes = encoding.GetBytes(value);
            return Convert.ToBase64String(bytes);
        }

        public static TResult GetClrType<TResult>(this string type,
            Func<Type, TResult> matched,
            Func<TResult> noMatch)
        {
            if (string.IsNullOrWhiteSpace(type))
                return noMatch();
            try
            {
                var typeClr = GetClrType(type);
                return matched(typeClr);
            }
            catch (InvalidDataException)
            {
                return noMatch();
            }
        }

        private static Type GetClrType(this string type)
        {
            if (type.ToLower() == "string")
                return typeof (string);
            if (type.ToLower() == "int")
                return typeof (int);
            if (type.ToLower() == "number")
                return typeof (decimal);
            if (type.ToLower() == "decimal")
                return typeof (decimal);
            if (type.ToLower() == "double")
                return typeof (double);
            if (type.ToLower() == "long")
                return typeof (long);
            if (type.ToLower() == "single")
                return typeof (float);
            if (type.ToLower() == "integer")
                return typeof (int);
            if (type.ToLower() == "int32")
                return typeof (int);
            if (type.ToLower() == "bool")
                return typeof (bool);
            if (type.ToLower() == "boolean")
                return typeof (bool);
            throw new InvalidDataException($"Type {type} not supported");
        }

        public static string EscapeSingleQuote(this string parameter)
        {
            return parameter.ToString().Trim('\'').Replace("\'", "\'\'");
        }

        public static bool IsGuid(this string possibleGuid)
        {
            Guid discard;
            return Guid.TryParse(possibleGuid, out discard);
        }

        public static string ToText(this byte [] bytes, System.Text.Encoding encoding = default(System.Text.Encoding))
        {
            if (default(System.Text.Encoding) == encoding)
                encoding = System.Text.ASCIIEncoding.ASCII;
            return encoding.GetString(bytes);
        }
    
        public static bool IsNullOrWhiteSpace(this string value)
        {
            return String.IsNullOrWhiteSpace(value);
        }
    }
}
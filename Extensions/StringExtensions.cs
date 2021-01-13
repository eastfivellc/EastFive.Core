using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EastFive.Linq;
using EastFive.Serialization;

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
            #region per https://tools.ietf.org/html/rfc4648#section-10 null/empty string are encoded as ""
            if (value.IsNullOrEmpty())
                return string.Empty;
            #endregion

            if (default(System.Text.Encoding) == encoding)
                encoding = System.Text.Encoding.UTF8;
            var bytes = encoding.GetBytes(value);
            return Convert.ToBase64String(bytes);
        }

        public static byte[] FromBase64String(this string base64String)
        {
            return Convert.FromBase64String(base64String);
        }

        public static bool TryParseBase64String(this string base64String, out byte[] bytes)
        {
            string incoming = base64String
                .Replace('_', '/')
                .Replace('-', '+');
            switch (base64String.Length % 4)
            {
                case 2: incoming += "=="; break;
                case 3: incoming += "="; break;
            }
            try
            {
                bytes = Convert.FromBase64String(incoming);
                return true;
            }catch(Exception)
            {
                bytes = default;
                return false;
            }
        }


        public static string ToBase64String(this byte[] bytes, bool urlSafe = false)
        {
            var encoding = Convert.ToBase64String(bytes);
            if (!urlSafe)
                return encoding;
            return encoding
                .Replace('+', '-')
                .Replace('/', '_')
                .Replace("=", "");
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

        public static Type GetClrType(this string type)
        {
            if (type.ToLower() == "string")
                return typeof (string);
            if (type.ToLower() == "int")
                return typeof (int);
            if (type.ToLower() == "count")
                return typeof(int);
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
            if (type.ToLower() == "text")
                return typeof(string);
            throw new InvalidDataException($"Type {type} not supported");
        }

        public static string GetClrString(this Type type)
        {
            if(type.IsNullable())
            {
                var baseTypeClrString = type
                    .GetNullableUnderlyingType()
                    .GetClrString();
                return $"{baseTypeClrString}?";
            }
            if (type == typeof(string))
                return "string";
            if (type == typeof(int))
                return "int";
            if (type == typeof(long))
                return "int";
            if (type == typeof(Int32))
                return "int";
            if (type == typeof(decimal))
                return "number";
            if (type == typeof(double))
                return "number";
            if (type == typeof(float))
                return "number";
            if (type == typeof(bool))
                return "boolean";
            return type.AssemblyQualifiedName;
            //throw new InvalidDataException($"Type {type} not supported");
        }

        public static string EscapeSingleQuote(this string parameter)
        {
            var trimmed = parameter.ToString()
                .Trim('\'');
            var characterScoped = new string(trimmed
                .Where(c => c > 26)
                .ToArray());
            return characterScoped
                .Replace("\\", "\\\\")
                .Replace("\'", "\'\'");
        }

        public static bool IsGuid(this string possibleGuid)
        {
            Guid discard;
            return Guid.TryParse(possibleGuid, out discard);
        }

        public static bool IsGuids(this string possibleGuids)
        {
            if (possibleGuids.IsNullOrWhiteSpace())
                return false;

            if (Guid.TryParse(possibleGuids, out Guid guidDiscard))
                return true;

            var possibleGuidArray = possibleGuids.Split(',');
            return possibleGuidArray.Any(possibleGuid => Guid.TryParse(possibleGuid, out Guid discard));
        }

        public static string ToText(this byte [] bytes, System.Text.Encoding encoding = default(System.Text.Encoding))
        {
            if (default(System.Text.Encoding) == encoding)
                encoding = System.Text.ASCIIEncoding.ASCII;
            return encoding.GetString(bytes);
        }

        public static Stream ToStream(this string text, System.Text.Encoding encoding = default(System.Text.Encoding))
        {
            var bytes = text.GetBytes(encoding);
            var stream = new MemoryStream(bytes);
            return stream;
        }

        public static bool IsNullOrEmpty(this string value)
        {
            return String.IsNullOrEmpty(value);
        }

        public static bool IsNullOrWhiteSpace(this string value)
        {
            return String.IsNullOrWhiteSpace(value);
        }

        public static bool HasBlackSpace(this string value)
        {
            return !String.IsNullOrWhiteSpace(value);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="value"></param>
        /// <param name="onIsNullOrWhiteSpace"></param>
        /// <param name="onHasContent">Passes back the string that was passed in to help keep the calling method from getting the params backwards</param>
        /// <returns></returns>
        public static TResult IsNullOrWhiteSpace<TResult>(this string value,
            Func<TResult> onIsNullOrWhiteSpace,
            Func<string, TResult> onHasContent)
        {
            return String.IsNullOrWhiteSpace(value) ?
                onIsNullOrWhiteSpace()
                :
                onHasContent(value);
        }

        public static TEnum AsEnum<TEnum>(this string value)
            where TEnum : struct // best we can do for now
        {
            return (TEnum)Enum.Parse(typeof(TEnum), value);
        }
    }
}
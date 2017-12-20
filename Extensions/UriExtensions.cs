using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BlackBarLabs
{
    public static class UriExtensions
    {
        public static Uri ToUri(this string uriString)
        {
            return new Uri(uriString);
        }

        public static bool AreEqual(this Uri uri1, Uri uri2,
            UriComponents partsToCompare = UriComponents.AbsoluteUri,
            UriFormat compareFormat = UriFormat.SafeUnescaped,
            StringComparison comparisonType = StringComparison.OrdinalIgnoreCase)
        {
            return Uri.Compare(uri1, uri2, partsToCompare, compareFormat, comparisonType) == 0;
        }

        public static bool AreEqual(this Uri uri1, string uri2String,
            UriComponents partsToCompare = UriComponents.AbsoluteUri,
            UriFormat compareFormat = UriFormat.SafeUnescaped,
            StringComparison comparisonType = StringComparison.OrdinalIgnoreCase)
        {
            var uri2 = new Uri(uri2String);
            return uri1.AreEqual(uri2);
        }

        public static string ToQueryString(this IDictionary<string, string> source)
        {
            // From: http://stackoverflow.com/questions/829080/how-to-build-a-query-string-for-a-url-in-c#5158497
            return String.Join("&", source.Select(kvp => String.Format("{0}={1}", HttpUtility.UrlEncode(kvp.Key), HttpUtility.UrlEncode(kvp.Value))).ToArray());
        }

        public static Uri SetQuery(this Uri uri, string query)
        {
            var uriWithQueryParam = new UriBuilder(uri) { Query = query };
            return uriWithQueryParam.Uri;
        }

        public static Uri SetQueryParam(this Uri uri, string name, string value)
        {
            // From: http://stackoverflow.com/questions/829080/how-to-build-a-query-string-for-a-url-in-c#20492373
            
            // this actually returns HttpValueCollection : NameValueCollection
            // which uses unicode compliant encoding on ToString()
            var queryParams = HttpUtility.ParseQueryString(uri.Query);
            if (queryParams.AllKeys.Contains(name))
                queryParams[name] = value;
            else
                queryParams.Add(name, value);
            var uriBuilder = new UriBuilder(uri)
            {
                Query = queryParams.ToString()
            };

            return uriBuilder.Uri;
        }

        public static bool TryGetQueryParam(this Uri uri, string name, out string param)
        {
            var queryParams = System.Web.HttpUtility.ParseQueryString(
               uri.Query);
            if (queryParams.AllKeys.Contains(name))
            {
                param = queryParams[name];
                return true;
            }
            param = default(string);
            return false;
        }

        public static string GetQueryParam(this Uri uri, string name)
        {
            return System.Web.HttpUtility.ParseQueryString(
               uri.Query)[name];
        }
    }
}

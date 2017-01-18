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

        public static Uri AddQuery(this Uri uri, string name, string value)
        {
            // From: http://stackoverflow.com/questions/829080/how-to-build-a-query-string-for-a-url-in-c#20492373
            
            // this actually returns HttpValueCollection : NameValueCollection
            // which uses unicode compliant encoding on ToString()
            var query = HttpUtility.ParseQueryString(uri.Query);
            query.Add(name, value);
            var uriBuilder = new UriBuilder(uri)
            {
                Query = query.ToString()
            };

            return uriBuilder.Uri;
        }
    }
}

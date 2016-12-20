using System;

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
    }
}

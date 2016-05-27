using System;

namespace BlackBarLabs.Core
{
    public static class UrnExtensions
    {
        public static Uri ToUrn(this Guid guid)
        {
            return new Uri(string.Format("urn:uuid:{0}", guid.ToString("D")));
        }

        public static Uri ToUrn(this string nsId, params object[] pathComponents)
        {
            return new Uri(string.Format("urn:{0}:{1}", nsId, string.Join(":", pathComponents)));
        }

        public static Guid ToGuid(this Uri uri)
        {
            var guidString = uri.GetUrnNamespaceString("uuid");
            if (guidString.Length != 1)
            {
                throw new ArgumentException("Could not understand format of UUID URN", "uri");
            }
            return Guid.Parse(guidString[0]);
        }

        /// <summary>
        /// The namespace-specific string of the URN for the namespace provided by
        /// <paramref name="nid"/>
        /// </summary>
        /// <returns>
        /// Namespace-specific string
        /// </returns>
        /// <param name='nid'>
        /// The urn namespace identifier.
        /// </param>
        public static string[] GetUrnNamespaceString(this Uri uri, string nid)
        {
            string uriNid;
            string[] nss = uri.ParseUrnNamespaceString(out uriNid);
            if (!uriNid.ToLower().Equals(nid.ToLower()))
            {
                throw new ArgumentException(String.Format("Urn has namespace identifier:[{0}], not namespace identifier [{1}].", uriNid, nid));
            }
            return nss;
        }

        public static bool IsUrn(this Uri uri)
        {
            return (uri.Scheme.ToLower().Equals("urn"));
        }

        public static bool TryParseUrnNamespaceString(this Uri uri, out string[] nss, out string nid)
        {
            try
            {
                nss = uri.ParseUrnNamespaceString(out nid);
            }
            catch (Exception)
            {
                nss = null;
                nid = null;
                return false;
            }
            return true;
        }

        public static string[] ParseUrnNamespaceString(this Uri uri, out string nid)
        {
            if (!uri.IsUrn())
            {
                throw new ArgumentException(String.Format("Uri has scheme:[{0}]. A URN is required. URNs have schema urn:", uri.Scheme));
            }
            var uriSegment = uri.Segments[0];
            var nidAndNss = uriSegment.Split(new char[] { ':' });
            if (nidAndNss.Length <= 1)
            {
                throw new ArgumentException(String.Format("Invalid URN:[{0}].", uriSegment));
            }
            nid = nidAndNss[0];
            var nss = new string[nidAndNss.Length - 1];
            Array.ConstrainedCopy(nidAndNss, 1, nss, 0, nss.Length);
            return nss;
        }

        public static string[] GetParams(this Uri uri, string ns)
        {
            var paramsStr = uri.ToString().Replace(string.Format("urn:{0}:", ns), string.Empty);
            return paramsStr.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
        }
    }
}
